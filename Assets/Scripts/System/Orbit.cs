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
public struct OrbitParameters
{
    [Tooltip("Nearest distance in orbit"), Range(0, 100)]
    public float periapsis;
    [Tooltip("Furthest distance in orbit"), Range(0, 100)]
    public float apoapsis;
    [Tooltip("Argument of periapsis (angle from ascending node)"), Range(0, 360)]
    public float angle;
    [Tooltip("Fraction of orbit to start at"), Range(0, 1)]
    public float offset;

    public enum OrbitDirection
    {
        CounterClockwise,
        Clockwise
    }
    public OrbitDirection direction;

    public float eccentricity => this.periapsis + this.apoapsis == 0 ? 0 : (this.apoapsis - this.periapsis) / (this.apoapsis + this.periapsis);
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

    public void Validate()
    {
        this.apoapsis = Mathf.Max(this.apoapsis, this.periapsis);
    }

    public struct OrbitPath
    {
        public Vector3[] path;
        public float dt;
        public float timeOffset;
        public OrbitDirection direction;
        public float period => this.path != null? this.dt * (this.path.Length + 1) : 0;

        static int ModPositive(int x, int m)
        {
            return (x % m + m) % m;
        }

        public Vector3 GetPosition(float t)
        {
            if (this.path == null || this.path.Length < 2)
            {
                return Vector3.zero;
            }

            float fIdx = this.direction == OrbitDirection.Clockwise
                ? (this.path.Length - ((t - this.timeOffset) / this.dt)) % this.path.Length
                : ((t + this.timeOffset) / this.dt) % this.path.Length;

            int idx0 = ModPositive(Mathf.FloorToInt(fIdx), this.path.Length);
            int idx1 = ModPositive(idx0 + 1, this.path.Length);
            float frac = fIdx - Mathf.FloorToInt(fIdx);
            return Vector3.Lerp(this.path[idx0], this.path[idx1], frac);
        }
    }

    public OrbitPath CalculatePath(float parentMass, float gravitationalConstant, float pathQuality, float fixedPeriod)
    {
        var orbit = new OrbitPhysics(this.periapsis, this.apoapsis, this.angle, parentMass, gravitationalConstant);

        if (orbit.valid)
        {
            // Intermediate steps, to improve accuracy
            const int SimSteps = 10;

            float dt = pathQuality * orbit.period / (1f * 360f);

            var pathList = new List<Vector3>();

            for (int itr = 0; itr < 5000 && orbit.angle < this.angle + 360f; ++itr)
            {
                var pos = orbit.GetPosition();
                pathList.Add(pos);
                for (int step = 0; step < SimSteps; step++)
                {
                    orbit.Step(dt / SimSteps);
                }
            }

            // If we closed the path successfully:
            if (Vector3.Distance(pathList[0], pathList[pathList.Count - 1]) < Vector3.Distance(pathList[0], pathList[1]) * 0.1f)
            {
                pathList.RemoveAt(pathList.Count - 1);
            }

            float period = fixedPeriod == 0 ? orbit.period : fixedPeriod;
            float finaldt = fixedPeriod == 0 ? dt : fixedPeriod / (pathList.Count + 1);
            return new OrbitPath { path = pathList.ToArray(), dt = finaldt, timeOffset = Mathf.Max(0, period * this.offset), direction = this.direction };
        }
        else
        {
            return new OrbitPath { };
        }
    }
}

public class Orbit : MonoBehaviour
{
    public OrbitParameters parameters = new OrbitParameters
    {
        apoapsis = 5,
        periapsis = 5,
        angle = 0
    };

    [Tooltip("Set a fixed orbital period (0 means use calculated one)"), Range(0, 600)]
    public float fixedPeriod;

    public GameConstants constants;

    [Tooltip("How finely subdivided the path rendering is"), Range(0, 1)]
    public float pathQuality = 1;

    [Tooltip("Transform to apply the position to, defaults to any child called 'Position'")]
    public Transform customPosition;
    [HideInInspector]
    public Transform position;

    [NonSerialized]
    public OrbitParameters.OrbitPath orbitPath;

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
        return FindObjectsOfType<SunLogic>().Select(s => s.GetComponent<GravitySource>().parameters.mass).Sum();
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
            var orbitControlled = GameObject.FindObjectsOfType<Orbit>().Where(o => o.isActiveAndEnabled).Select(o => o.position.gameObject);
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

        this.parameters.Validate();

        Debug.Assert(ValidateParents());

        this.orbitPath = this.parameters.CalculatePath(this.FindParentsMass(), this.constants.GravitationalConstant, this.pathQuality, this.fixedPeriod);

        this.UpdatePosition(0);
    }

    public void SimUpdate(float simTime)
    {
        this.UpdatePosition(simTime);
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
        if(this.pathPositions != null)
        {
            lineRenderer.positionCount = this.pathPositions.Length;
            lineRenderer.SetPositions(this.pathPositions);
        }
        else
        {
            lineRenderer.positionCount = 0;
        }
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
