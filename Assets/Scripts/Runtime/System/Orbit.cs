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

    public static readonly OrbitParameters Zero = new OrbitParameters();

    public bool validOrbit => this.periapsis > 0 && this.apoapsis > 0;

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

            float fIdx = (this.direction == OrbitDirection.Clockwise
                ? (this.path.Length - (t - this.timeOffset) / this.dt)
                : ((t + this.timeOffset) / this.dt)) % this.path.Length;

            int idx0 = ModPositive(Mathf.FloorToInt(fIdx), this.path.Length);
            int idx1 = ModPositive(idx0 + 1, this.path.Length);
            float frac = fIdx - Mathf.FloorToInt(fIdx);
            return Vector3.Lerp(this.path[idx0], this.path[idx1], frac);
        }

        public (Vector3, Vector3) GetPositionVelocity(float t)
        {
            if (this.path == null || this.path.Length < 2)
            {
                return (Vector3.zero, Vector3.zero);
            }

            float fIdx = (this.direction == OrbitDirection.Clockwise
                ? (this.path.Length - (t - this.timeOffset) / this.dt)
                : ((t + this.timeOffset) / this.dt)) % this.path.Length;

            int idx0 = ModPositive(Mathf.FloorToInt(fIdx), this.path.Length);
            int idx1 = ModPositive(idx0 + 1, this.path.Length);
            float frac = fIdx - Mathf.FloorToInt(fIdx);
            var position = Vector3.Lerp(this.path[idx0], this.path[idx1], frac);
            var velocity = (Vector3.Lerp(this.path[idx1], this.path[(idx1 + 1) % this.path.Length], frac) - position) / this.dt;
            return (position, velocity);
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
            return new OrbitPath {
                path = pathList.ToArray(),
                dt = finaldt,
                timeOffset = Mathf.Max(0, period * this.offset),
                direction = this.direction
            };
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

    public Vector3 velocity;

    void OnValidate()
    {
        this.RefreshValidateRecursive();
    }

    void Awake()
    {
        this.position = this.customPosition == null ? this.transform.Find("Position") : this.customPosition;
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

    float FindParentsMass()
    {
        var directParent = this.gameObject.GetComponentInParentOnly<GravitySource>();
        if (directParent != null)
            return directParent.parameters.mass;
        // Instead use sum of sun masses...
        return FindObjectsOfType<StarLogic>().Select(s => s.GetComponent<GravitySource>().parameters.mass).Sum();
    }

    public void RefreshValidateRecursive()
    {
        this.RefreshValidate();

        foreach (var child in this.GetComponentsInChildren<Orbit>().Where(c => c.isActiveAndEnabled && c != this))
        {
            child.RefreshValidateRecursive();
        }
    }

    void RefreshValidate()
    {
        if (!this.isActiveAndEnabled)
        {
            this.position.localPosition = Vector3.zero;
            return;
        }

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

        // Scale must be 1
        this.transform.localScale = Vector3.one;
        // Rotation must be 0
        this.transform.localRotation = Quaternion.identity;

        // If we are not parented to another orbit then position must be 0,
        // otherwise our position will be set by the orbit
        if (this.GetComponentInParentOnly<Orbit>() == null)
        {
            this.transform.localPosition = Vector3.zero;
        }

        this.orbitPath = this.parameters.CalculatePath(this.FindParentsMass(), this.constants.GravitationalConstant, this.pathQuality, this.fixedPeriod);

        this.UpdatePosition(0);
    }

    public void SimUpdate(int simTick)
    {
        Debug.Assert(this.isActiveAndEnabled);
        this.UpdatePosition(simTick * Time.fixedDeltaTime);
    }

    void UpdatePosition(float time)
    {
        (this.position.localPosition, this.velocity) = this.orbitPath.GetPositionVelocity(time);
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
