using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;


// http://hyperphysics.phy-astr.gsu.edu/hbase/kepler.html
// This method of calculation is not so accurate, it works okay for eccentricity < 0.2 (which covers all planets in our solar system, and pluto).
// TODO: use a more accurate method (although they will all be slower, we could just precalculate the path and then interpolate along it).
// References/code examples for alternate methods:
// https://space.stackexchange.com/questions/8911/determining-orbital-position-at-a-future-point-in-time
// https://pastebin.com/NqTwa4QM
// https://space.stackexchange.com/questions/21458/is-this-c-code-to-obtain-the-coordinates-of-the-planets-correct/
// https://github.com/nemesgyadam/all-my-circuirs/blob/master/Orbit.cs
// http://www.jgiesen.de/kepler/kepler.html

[Serializable]
public struct OrbitParameters
{
    // Longitude means rotational offset from the primary axis of the system in orbital mechanics.
    // e.g. if rotation is around z axis then either x or y might be the primary axis from which longitudes are measured.
    // Perihelion is the point of closest approach to the orbited body.
    [Tooltip("The angle from primary axis of the point of closest approach, in degrees"), Range(0, 360)]
    public float longitudeOfPerihelion;
    [Tooltip("Average orbital distance"), Range(0, 200)]
    public float meanDistance;
    [Tooltip("Motion of orbiting body per second, in degrees per second"), Range(-45, 45)]
    public float motionPerSecond;
    [Tooltip("How elliptical the orbit is (values beyond 0.2 become inaccurate with the method of calculation used here)"), Range(0, 0.3f)]
    public float eccentricity;
    // Mean longitude is the ecliptic longitude at which an orbiting body could be found if its orbit were circular, and free of perturbations, and if its inclination were zero
    [Tooltip("Angle the orbit starts from, in degrees"), Range(0, 360)]
    public float meanLongitude;

    static float Mod2PI(float val)
    {
        while (val > Mathf.PI * 2f)
            val -= Mathf.PI * 2f;
        while (val < 0)
            val += Mathf.PI * 2f;
        return val;
    }

    public Vector2 GetPosition(float time, float meanLongitudeOffset = 0)
    {
        float meanAnomaly = Mod2PI((this.motionPerSecond * time + this.meanLongitude + meanLongitudeOffset - this.longitudeOfPerihelion) * Mathf.Deg2Rad);
        float trueAnomaly = Mod2PI(meanAnomaly + 1f * ((2f * this.eccentricity - Mathf.Pow(this.eccentricity, 3) / 4f) * Mathf.Sin(meanAnomaly)
            + (5f / 4f) * Mathf.Pow(this.eccentricity, 2) * Mathf.Sin(2f * meanAnomaly) + (13f / 12f) * Mathf.Pow(this.eccentricity, 3) * Mathf.Sin(3f * meanAnomaly)));
        float radiusVector = this.meanDistance * (1f - Mathf.Pow(this.eccentricity, 2)) / (1f + this.eccentricity * Mathf.Cos(trueAnomaly));
        return new Vector2(
            radiusVector * Mathf.Cos(trueAnomaly + this.longitudeOfPerihelion * Mathf.Deg2Rad),
            radiusVector * Mathf.Sin(trueAnomaly + this.longitudeOfPerihelion * Mathf.Deg2Rad)
        );
    }
}

public class Orbit : MonoBehaviour
{
    public OrbitParameters parameters = new OrbitParameters
    {
        longitudeOfPerihelion = 0,
        meanDistance = 5,
        motionPerSecond = 0,
        eccentricity = 0,
        meanLongitude = 0
    };

    public GameConstants constants;

    [Tooltip("How finely subdivided the path rendering is"), Range(0, 1)]
    public float pathQuality = 1;

    [Tooltip("Transform to apply the position to, defaults to any child called 'Position'")]
    public Transform customPosition;
    [HideInInspector]
    public Transform position;

    [Tooltip("Whether to automatically adjust motionPerSecond orbit parameter based on meanDistance")]
    public bool autoMotionPerSecond = true;
    [Tooltip("Whether automatically motion is clockwise")]
    public bool clockwiseMotion = false;

#if UNITY_EDITOR
    Vector3[] _editorPath;
    public Vector3[] editorPath
    {
        get
        {
            if (this._editorPath == null)
            {
                this._editorPath = this.GetPositions(360, 0.2f);
            }
            return _editorPath;
        }
    }
#endif

    // This was used to more closely match the SimManager math by simply adding positions to get world location for
    // nested orbits. However it doesn't appear to be necessary and can be removed at a later time if the path
    // sim proves stable.
    //Orbit parent;

    void OnValidate()
    {
        this.RefreshValidateRecursive();
    }

    void Start()
    {
        this.RefreshValidate();
        this.UpdateOrbitPath();
    }

    void Update()
    {
        this.UpdateOrbitWidth();
    }

    float LawOfPeriods(float parentMass, float meanDistance)
    {
        return Mathf.Sqrt(4f * Mathf.Pow(Mathf.PI, 2f) * Mathf.Pow(meanDistance, 3f) / (parentMass * this.constants.GravitationalConstant));
    }

    float FindParentsMass()
    {
        var directParent = this.gameObject.GetComponentInParentOnly<GravitySource>();
        if (directParent != null)
            return directParent.parameters.mass;
        // Instead use sum of sun masses...
        return GameObject.FindObjectsOfType<SunLogic>().Select(s => s.GetComponent<GravitySource>().parameters.mass).Sum();
    }

    public void RefreshValidateRecursive()
    {
        this.RefreshValidate();

        foreach (var child in this.GetComponentsInChildren<Orbit>().Where(c => c != this))
        {
            child.RefreshValidateRecursive();
        }
    }

    void RefreshValidate()
    {
        Assert.IsNotNull(this.constants);

        this.position = this.customPosition == null ? this.transform.Find("Position") : this.customPosition;
        bool ValidateParents()
        {
            // Only game objects being controlled by Orbit component are allowed to have non-identity transforms
            var orbitControlled = GameObject.FindObjectsOfType<Orbit>().Select(o => o.position.gameObject);
            var invalidParents = this.GetAllParents()
                .Where(p => !orbitControlled.Contains(p) && !p.transform.IsIdentity());
            if (invalidParents.Any())
            {
                foreach (var p in invalidParents)
                {
                    Debug.LogError($"{this}: parent {p.name} is invalid, it has none zero transform", p);
                }
                return false;
            }
            return true;
        }

        Debug.Assert(ValidateParents());

        this.parameters.eccentricity = Mathf.Clamp(this.parameters.eccentricity, 0, 0.3f);

        if (this.autoMotionPerSecond)
        {
            this.parameters.motionPerSecond = this.parameters.meanDistance > 0?
                (this.clockwiseMotion? -360f : 360f) / this.LawOfPeriods(this.FindParentsMass(), this.parameters.meanDistance)
                :
                0f;
        }

        this.UpdatePosition(0);

#if UNITY_EDITOR
        this._editorPath = null;
#endif
    }

    public void SimUpdate()
    {
        this.UpdatePosition(GameLogic.Instance.simTime);
    }

    void UpdatePosition(float time)
    {
        // See note on the parent variable declaration above
        //var newPosition = (Vector3)this.parameters.GetPosition(time);
        //if (this.parent != null)
        //{
        //    newPosition += this.parent.position.position;
        //}
        //this.position.position = newPosition;
        this.position.localPosition = this.parameters.GetPosition(time);
    }

    public Vector3[] GetPositions(float degrees = 360f, float quality = 1)
    {
        degrees = Mathf.Clamp(degrees, -360f, 360f);
        int numPoints = Math.Max(1, (int)(Mathf.Abs(degrees) * this.pathQuality * quality));
        var path = new Vector3[numPoints];
        float timePerPoint = degrees / (numPoints - 1);
        for (int i = 0; i < numPoints; i++)
        {
            path[i] = this.parameters.GetPosition(0, i * timePerPoint);
        }
        return path;
    }

    void UpdateOrbitPath()
    {
        var lineRenderer = this.GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            return;
        }

        var path = this.GetPositions();
        lineRenderer.positionCount = path.Length;
        lineRenderer.SetPositions(path);
    }

    void UpdateOrbitWidth()
    {
        var lineRenderer = this.GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            return;
        }
        lineRenderer.startWidth = lineRenderer.endWidth = this.constants.OrbitLineWidth;
    }

}
