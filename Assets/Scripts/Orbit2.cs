using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;


// Simulated orbit with baking
// Uses:
// - elliptical orbit of body -- baked
// - ballistic trajectory -- baked for specified range (in time), then simulated

// bodies:
// - sun/planet/moon/comet -- one gravity source effects it, elliptical orbit -- can be baked
// - player -- no parent, can exert force -- needs to be simulated to show lookahead path, but not baked
// - parabolic trajectory -- 
[Serializable]
public struct OrbitParameters2
{
    public float periapsis; // nearest distance in orbit
    public float apoapsis; // furthest distance in orbit
    public float angle; // argument of periapsis (angle from ascending node)
    public float offset; // 0 - 1, how far through one orbit to start the path
    public enum OrbitDirection
    {
        CounterClockwise,
        Clockwise
    }
    public OrbitDirection direction;

    public float eccentricity => this.periapsis + this.apoapsis == 0? 0 : (this.apoapsis - this.periapsis) / (this.apoapsis + this.periapsis);
    public float semiMajorAxis => (this.apoapsis + this.periapsis) / 2f;

    public void SetPeriapsis(float newPeriapsis)
    {
        this.periapsis = newPeriapsis;
        this.apoapsis = Mathf.Max(this.apoapsis, newPeriapsis);
    }

    public void SetApoapsis(float newApoapsis)
    {
        this.apoapsis = newApoapsis;
        this.periapsis = Mathf.Min(this.periapsis, newApoapsis);
    }

    public void SetPeriapsisMaintainEccentricity(float newPeriapsis)
    {
        float oldEccentricity = this.eccentricity;
        this.periapsis = newPeriapsis;
        if (newPeriapsis == 0)
        {
            this.apoapsis = 0;
        }
        else
        {
            // a = (1 - e) / ((1 + e) * p);
            // https://en.wikipedia.org/wiki/Orbital_eccentricity#:~:text=For%20elliptical%20orbits%20it%20can,a%20focus%20of%20the%20ellipse).
            this.apoapsis = (1f + oldEccentricity) * this.periapsis / (1f - oldEccentricity);
        }
    }

    public struct OrbitPath
    {
        public Vector3[] path;
        public float dt;
        public float timeOffset;
        public OrbitDirection direction;

        static int ModPositive(int x, int m)
        {
            return (x % m + m) % m;
        }
        static float ModPositive(float x, float m)
        {
            return (x % m + m) % m;
        }

        public Vector3 GetPosition(float t)
        {
            if(this.path.Length < 2)
            {
                return Vector3.zero;
            }
            float fIdx;
            if (this.direction == OrbitDirection.Clockwise)
            {
                fIdx = (this.path.Length - ((t - this.timeOffset) / this.dt)) % this.path.Length;
            }
            else
            {
                fIdx = ((t + this.timeOffset) / this.dt) % this.path.Length;
            }
            //float fIdx = ((t + this.timeOffset) / this.dt) % this.path.Length;
            //if(this.direction == OrbitDirection.Clockwise)
            //{
                //fIdx = this.path.Length - fIdx;
            //}
            int idx0 = ModPositive(Mathf.FloorToInt(fIdx), this.path.Length);
            int idx1 = ModPositive(idx0 + 1, this.path.Length);
            float frac = fIdx - Mathf.FloorToInt(fIdx);
            return Vector3.Lerp(this.path[idx0], this.path[idx1], frac);
        }
    }

    public OrbitPath CalculatePath(float parentMass, float gravitationalConstant, float pathQuality)
    {
        var orbit = new OrbitPhysics(this.periapsis, this.apoapsis, this.angle, parentMass, gravitationalConstant);

        // Intermediate steps, to improve accuracy
        const int SimSteps = 10;

        float dt = pathQuality * orbit.period / (1f * 360f);

        var pathList = new List<Vector3>();

        for(int itr = 0; itr < 5000 && orbit.angle < this.angle + 360f; ++itr)
        {
            var pos = orbit.GetPosition();
            pathList.Add(pos);
            for (int step = 0; step < SimSteps; step++)
            {
                orbit.Step(dt / SimSteps);
            }
        }

        // If we closed the path successfully:
        if(Vector3.Distance(pathList[0], pathList[pathList.Count - 1]) < Vector3.Distance(pathList[0], pathList[1]) * 0.1f)
        {
            pathList.RemoveAt(pathList.Count - 1);
        }

        return new OrbitPath { path = pathList.ToArray(), dt = dt, timeOffset = Mathf.Max(0, orbit.period * this.offset), direction = this.direction };
    }
}

public class Orbit2 : MonoBehaviour
{
    public OrbitParameters2 parameters = new OrbitParameters2
    {
        apoapsis = 5,
        periapsis = 5,
        angle = 0
    };

    public GameConstants constants;

    [Tooltip("How finely subdivided the path rendering is"), Range(0, 1)]
    public float pathQuality = 1;

    [Tooltip("Transform to apply the position to, defaults to any child called 'Position'")]
    public Transform customPosition;
    [HideInInspector]
    public Transform position;

    public OrbitParameters2.OrbitPath orbitPath;

    public Vector3[] pathPositions => this.orbitPath.path;

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
        this.CreateOrbitPath();
    }

    void Update()
    {
        this.UpdateOrbitWidth();
    }

    //float LawOfPeriods(float parentMass, float meanDistance)
    //{
    //    return Mathf.Sqrt(4f * Mathf.Pow(Mathf.PI, 2f) * Mathf.Pow(meanDistance, 3f) / (parentMass * this.constants.GravitationalConstant));
    //}

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

        this.orbitPath = this.parameters.CalculatePath(this.FindParentsMass(), this.constants.GravitationalConstant, this.pathQuality);

        this.UpdatePosition(0);
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
        this.position.localPosition = this.orbitPath.GetPosition(time);
    }

    void CreateOrbitPath()
    {
        var lineRenderer = this.GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            return;
        }
        lineRenderer.positionCount = this.pathPositions.Length;
        lineRenderer.SetPositions(this.pathPositions);
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
