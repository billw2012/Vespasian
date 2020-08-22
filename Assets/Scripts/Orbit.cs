using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

// http://hyperphysics.phy-astr.gsu.edu/hbase/kepler.html
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
    [Tooltip("Motion of orbiting body per second, in degrees per second"), Range(0, 45)]
    public float motionPerSecond;
    [Tooltip("How elliptical the orbit is"), Range(0, 1)]
    public float eccentricity;
    // Mean longitude is the ecliptic longitude at which an orbiting body could be found if its orbit were circular, and free of perturbations, and if its inclination were zero
    [Tooltip("Angle the orbit starts from, in degrees"), Range(0, 360)]
    public float meanLongitude;

    static float Mod2PI(float val)
    {
        while (val > Mathf.PI * 2)
            val -= Mathf.PI * 2;
        while (val < 0)
            val += Mathf.PI * 2;
        return val;
    }

    public Vector2 GetPosition(float time, float meanLongitudeOffset = 0)
    {
        float meanAnomaly = Mod2PI((this.motionPerSecond * time + this.meanLongitude + meanLongitudeOffset - this.longitudeOfPerihelion) * Mathf.Deg2Rad);
        float trueAnomaly = meanAnomaly + 1 * ((2 * this.eccentricity - Mathf.Pow(this.eccentricity, 3) / 4) * Mathf.Sin(meanAnomaly)
            + 5 / 4 * Mathf.Pow(this.eccentricity, 2) * Mathf.Sin(2 * meanAnomaly) + 13 / 12 * Mathf.Pow(this.eccentricity, 3) * Mathf.Sin(3 * meanAnomaly));
        float radiusVector = this.meanDistance * (1 - Mathf.Pow(this.eccentricity, 2)) / (1 + this.eccentricity * Mathf.Cos(trueAnomaly));
        return new Vector2(
            radiusVector * Mathf.Cos(trueAnomaly + this.longitudeOfPerihelion * Mathf.Deg2Rad),
            radiusVector * Mathf.Sin(trueAnomaly + this.longitudeOfPerihelion * Mathf.Deg2Rad)
        );
    }
}

public class Orbit : MonoBehaviour
{
    public OrbitParameters parameters = new OrbitParameters {
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

    // This was used to more closely match the SimManager math by simply adding positions to get world location for
    // nested orbits. However it doesn't appear to be necessary and can be removed at a later time if the path
    // sim proves stable.
    //Orbit parent;

    void OnValidate()
    {
        this.RefreshValidate();
        foreach(var child in this.GetComponentsInChildren<Orbit>())
        {
            child.RefreshValidate();
        }
    }

    void Start()
    {
        this.RefreshValidate();
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

        if (this.autoMotionPerSecond)
        {
            this.parameters.motionPerSecond = this.parameters.meanDistance > 0 ? 360f / LawOfPeriods(this.FindParentsMass(), this.parameters.meanDistance) : 0f;
        }

        this.UpdatePosition(0);
        this.UpdateOrbitPath();
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

    void UpdateOrbitPath()
    {
        var lineRenderer = this.GetComponent<LineRenderer>();
        if(lineRenderer == null)
        {
            return;
        }

        var path = new List<Vector3>();
        if (Application.isPlaying)
        {
            int pathPoints = (int)(360 * this.pathQuality);
            //float totalOrbitTime = 360 / this.parameters.motionPerSecond;
            float timePerPoint = 360f / pathPoints;
            for (int i = 0; i < pathPoints; i++)
            {
                path.Add(this.parameters.GetPosition(0, i * timePerPoint));
            }
        }
        lineRenderer.positionCount = path.Count;
        lineRenderer.SetPositions(path.ToArray());
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
