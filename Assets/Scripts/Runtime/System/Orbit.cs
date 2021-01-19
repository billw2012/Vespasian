using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;


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

    public float eccentricity => this.periapsis + this.apoapsis == 0
        ? 0
        : (this.apoapsis - this.periapsis) / (this.apoapsis + this.periapsis);

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
        public float period => this.path != null ? this.dt * (this.path.Length + 1) : 0;

        private static int ModPositive(int x, int m)
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
                ? this.path.Length - (t - this.timeOffset) / this.dt
                : (t + this.timeOffset) / this.dt) % this.path.Length;

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
                ? this.path.Length - (t - this.timeOffset) / this.dt
                : (t + this.timeOffset) / this.dt) % this.path.Length;

            int idx0 = ModPositive(Mathf.FloorToInt(fIdx), this.path.Length);
            int idx1 = ModPositive(idx0 + 1, this.path.Length);
            float frac = fIdx - Mathf.FloorToInt(fIdx);
            var position = Vector3.Lerp(this.path[idx0], this.path[idx1], frac);
            var velocity = (Vector3.Lerp(this.path[idx1], this.path[(idx1 + 1) % this.path.Length], frac) - position) /
                           this.dt;
            // If we are going in the opposite direction then reverse the velocity of course
            if (this.direction == OrbitDirection.Clockwise)
            {
                velocity *= -1;
            }

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
            if (Vector3.Distance(pathList[0], pathList[pathList.Count - 1]) <
                Vector3.Distance(pathList[0], pathList[1]) * 0.1f)
            {
                pathList.RemoveAt(pathList.Count - 1);
            }

            float period = fixedPeriod == 0 ? orbit.period : fixedPeriod;
            float finaldt = fixedPeriod == 0 ? dt : fixedPeriod / (pathList.Count + 1);
            return new OrbitPath
            {
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

[Serializable]
public struct AnalyticOrbit
{
    [Tooltip("The angle from primary axis of the point of closest approach, in degrees"), Range(0, 360)]
    public float argumentOfPeriapsis;
    [Tooltip("Average orbital distance"), Range(0, 200)]
    public float semiMajorAxis;
    [Tooltip("Motion of orbiting body per second, in degrees per second"), Range(-45, 45)]
    public float motionPerSecond;
    [Tooltip("How elliptical the orbit is (values beyond 0.2 become inaccurate with the method of calculation used here)"), Range(0, 0.3f)]
    public float eccentricity;

    public float directionSign => Mathf.Sign(this.motionPerSecond);

    public bool isElliptic => this.eccentricity <= 1;
    // orbits with eccentricity near 1 are unstable in this system due to division by very small numbers occurring
    public bool isUnstable => Mathf.Abs(this.eccentricity - 1) < 0.01f;

    /// <summary>
    /// Low point of the orbit
    /// </summary>
    public float periapsis => OrbitalUtils.Periapsis(this.semiMajorAxis, this.eccentricity);
    /// <summary>
    /// High point of the orbit
    /// </summary>
    public float apoapsis => OrbitalUtils.Apoapsis(this.semiMajorAxis, this.eccentricity);

    /// <summary>
    /// Either high or low point of the orbit, whichever comes soonest
    /// </summary>
    public float nextapsis => this.timeOfPeriapsis <= this.timeOfApoapsis ? this.periapsis : this.apoapsis;

    public float period => 360f / Mathf.Abs(this.motionPerSecond);
    public float timeOfPeriapsis => 
        !this.isElliptic
        ? (this.meanLongitude / -this.motionPerSecond)
        : (this.meanLongitude / -this.motionPerSecond + this.period) % this.period;
    public float timeOfApoapsis => 
        !this.isElliptic
        ? float.PositiveInfinity
        : ((this.meanLongitude + 180f) / -this.motionPerSecond + this.period) % this.period;
    public bool isDescending => this.timeOfPeriapsis > 0 && this.timeOfPeriapsis < this.timeOfApoapsis;
    public float timeOfNextapsis => Mathf.Min(this.timeOfPeriapsis, this.timeOfApoapsis);
    
    // Mean longitude is the ecliptic longitude at which an orbiting body could be found if its orbit were circular, and free of perturbations, and if its inclination were zero
    public float meanLongitude;

    private static float Mod2PI(float val)
    {
        while (val > Mathf.PI * 2f)
            val -= Mathf.PI * 2f;
        while (val < 0)
            val += Mathf.PI * 2f;
        return val;
    }

    public Vector2 GetPosition(float time, float meanLongitudeOffset = 0)
    {
        float meanAnomaly = (this.motionPerSecond * time + meanLongitudeOffset * this.directionSign + this.meanLongitude) * Mathf.Deg2Rad;
        if (this.isElliptic)
        {
            meanAnomaly = Mod2PI(meanAnomaly);
        }
        float trueAnomaly = OrbitalUtils.MeanToTrueAnomaly(meanAnomaly, this.eccentricity);

        // float trueAnomaly = Mod2PI(meanAnomaly +
        //                            1f *
        //                            ((2f * this.eccentricity -
        //                              Mathf.Pow(this.eccentricity, 3) / 4f) * Mathf.Sin(meanAnomaly) +
        //                             (5f / 4f) * Mathf.Pow(this.eccentricity, 2) * Mathf.Sin(2f * meanAnomaly) +
        //                             (13f / 12f) * Mathf.Pow(this.eccentricity, 3) * Mathf.Sin(3f * meanAnomaly)));
        float d = 1f + this.eccentricity * Mathf.Cos(trueAnomaly);
        float radiusVector = d == 0? 0 : this.semiMajorAxis * (1f - Mathf.Pow(this.eccentricity, 2)) / d;
        return new Vector2(
            radiusVector * Mathf.Cos(trueAnomaly + this.argumentOfPeriapsis * Mathf.Deg2Rad),
            radiusVector * Mathf.Sin(trueAnomaly + this.argumentOfPeriapsis * Mathf.Deg2Rad)
        );
        
        // double gravParameter = this.referenceBody.gravParameter;
        // double c = Math.Cos(tA);
        // double s = Math.Sin(tA);
        // double f = this.semiMajorAxis * (1.0 - this.eccentricity * this.eccentricity) / (1.0 + this.eccentricity * c);
        // double d = c * f;
        // double d2 = s * f;
        //
        // state.pos = this.OrbitFrame.X * d + this.OrbitFrame.Y * d2;
    }
    
    public Vector3 GetPeriapsisPosition() => this.GetPosition(this.timeOfPeriapsis);
    public Vector3 GetApoapsisPosition() => this.GetPosition(this.timeOfApoapsis);
    
    public Vector3 GetNextapsisPosition() => this.GetPosition(this.timeOfNextapsis);

    public Vector3[] GetPath(int resolution = 180)
    {
        // Only join the start to the end if we are an elliptical orbit (not para/hyperbolic)
        float dt =  this.isElliptic? 360f / (resolution - 1) : 360f / resolution;
        var path = new Vector3[resolution];
        for (int i = 0; i < resolution; ++i)
        {
            path[i] = this.GetPosition(0, i * dt);
        }
        return path;
    }

    public static AnalyticOrbit FromCartesianStateVector(Vector3 r, Vector3 v, float mass, float G)
    {
        var h = Vector3.Cross(r, v); //angular_momentum(r, v);
        //var n = Vector3.Cross(Vector3.forward, h); //node_vector(h);

        float mu = mass * G;
        var eccVec = 1 / mu * ((v.sqrMagnitude - mu / r.magnitude) * r - Vector3.Dot(r, v) * v); //eccentricity_vector(r, v, mu);

        float orbitalEnergy = v.sqrMagnitude / 2 - mu / r.magnitude; //specific_orbital_energy(r, v, mu);

        float eccentricity = eccVec.magnitude;
        float semiMajorAxis = 0;
        if (eccentricity > 1f)
        {
            semiMajorAxis = -(h.sqrMagnitude / mu) / (eccVec.sqrMagnitude - 1f);
        }
        else
        {
            semiMajorAxis = -mu / (2 * orbitalEnergy);
        }

        const double SMALL_NUMBER = 1e-15;

        // Argument of periapsis is the angle between eccentricity vector and its x component.
        float argumentOfPeriapsis = Mathf.Abs(eccentricity) < SMALL_NUMBER ? 0 : Vector3.SignedAngle(Vector3.right, eccVec, Vector3.forward) * Mathf.Deg2Rad;
        
        float trueAnomaly = 0;
        if (Mathf.Abs(eccentricity) < SMALL_NUMBER)
        {
            // True anomaly is angle between position
            // vector and its x component.
            trueAnomaly = Mathf.Acos(r.x / r.magnitude);
            if( v.x > 0)
            {
                trueAnomaly = 2 * Mathf.PI - trueAnomaly;
            }
        }
        else
        {
            // if (eccVec.z < 0)
            // {
            //     argumentOfPeriapsis = 2 * Mathf.PI - argumentOfPeriapsis;
            // }
            
            trueAnomaly = Vector3.SignedAngle(eccVec, r, Vector3.forward) * Mathf.Deg2Rad;
        }

        float meanLongitude = OrbitalUtils.TrueToMeanAnomaly(trueAnomaly, eccentricity) * Mathf.Rad2Deg;
        return new AnalyticOrbit
        {
            semiMajorAxis = semiMajorAxis,
            eccentricity = eccentricity,
            //meanLongitude = //OrbitalUtils.EccentricToTrueAnomaly((float)OrbitalUtils.MeanToEccentricAnomaly(arg_pe - tA, e), e) * Mathf.Rad2Deg,
            //meanLongitude = OrbitalUtils. tA * Mathf.Rad2Deg,
            meanLongitude = float.IsInfinity(meanLongitude)? 0 : meanLongitude,
            argumentOfPeriapsis = argumentOfPeriapsis * Mathf.Rad2Deg,
            motionPerSecond = Mathf.Sign(h.z) * OrbitalUtils.MeanMotion(semiMajorAxis, mass, G) * Mathf.Rad2Deg,
        };
        //OrbitalElements(a=a, e=e, i=i, raan=raan, arg_pe=arg_pe, f=f);
    }

    /// <summary>
    /// Determine when orbit <paramref name="a"/> approaches to <paramref name="distance"/> of <paramref name="b"/>,
    /// within timeframe specified by <paramref name="t0"/> and <paramref name="t1"/>.
    /// This could be used to determine SOI intercept time, or collisions.
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="t0"></param>
    /// <param name="t1"></param>
    /// <param name="distance"></param>
    /// <param name="tolerance"></param>
    /// <returns></returns>
    public static (bool occurred, float t) Intercept(AnalyticOrbit a, AnalyticOrbit b, float t0, float t1, float distance, float tolerance = 0.1f)
    {
        return ((bool occurred, float t)) MathX.FindRoot(t => (a.GetPosition((float)t) - b.GetPosition((float)t)).magnitude - distance,
            t0, t1, tolerance);
    }

    public void DebugDraw(Vector3 position, Color color, int segments = 180, float duration = 0f)
    {
        var orbitPath = this.GetPath();
        for (int i = 0; i < orbitPath.Length - 1; i++)
        {
            Debug.DrawLine(position + orbitPath[i], position + orbitPath[i+1], color, 1f);   
        }
    }

    public void DumpToLog()
    {
        var stableStr = this.isUnstable ? "Unstable" : "Stable";
        Debug.Log($"a {this.semiMajorAxis} / e {this.eccentricity} / d {this.directionSign} / argpe {this.argumentOfPeriapsis} / {stableStr}");
    }
}

/// <summary>
/// Describes and simulates the orbit of a Body around a parent GravitySource (determined automatically)
/// </summary>
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

    [NonSerialized]
    public Transform position;

    [NonSerialized]
    public OrbitParameters.OrbitPath orbitPath;

    public Vector3[] pathPositions => this.orbitPath.path;

    [NonSerialized]
    public Vector3 relativeVelocity;

    [NonSerialized]
    public Vector3 absoluteVelocity;

    private Orbit parentOrbit;

    private void OnValidate()
    {
        this.Awake();
        this.RefreshValidateRecursive();
    }

    private void Awake()
    {
        this.position = this.customPosition == null ? this.transform.Find("Position") : this.customPosition;
    }

    private void Start()
    {
        this.RefreshValidate();
        this.CreateOrbitPath();
    }

    private void Update()
    {
        this.UpdateOrbitWidth();
    }

    private float FindParentsMass()
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

    private void RefreshValidate()
    {
        if (!this.isActiveAndEnabled)
        {
            if (this.position != null)
            {
                this.position.localPosition = Vector3.zero;
            }
            return;
        }

        this.parameters.Validate();

        // Scale must be 1
        this.transform.localScale = Vector3.one;
        // Rotation must be 0
        this.transform.localRotation = Quaternion.identity;

        this.parentOrbit = this.GetComponentInParentOnly<Orbit>();

        // If we are not parented to another orbit then position must be 0,
        // otherwise our position will be set by the orbit
        if (this.parentOrbit == null)
        {
            this.transform.localPosition = Vector3.zero;
        }

        this.orbitPath = this.parameters.CalculatePath(this.FindParentsMass(), this.constants.GravitationalConstant, this.pathQuality, this.fixedPeriod);

        this.UpdatePosition(0);
    }

    public void SimUpdate(Simulation simulation, int simTick)
    {
        Debug.Assert(this.isActiveAndEnabled);
        this.UpdatePosition(simTick * Time.fixedDeltaTime);
    }

    private void UpdatePosition(float time)
    {
        if (this.position != null)
        {
            (this.position.localPosition, this.relativeVelocity) = this.orbitPath.GetPositionVelocity(time);
            this.absoluteVelocity = this.parentOrbit != null 
                ? this.parentOrbit.absoluteVelocity + this.relativeVelocity 
                : this.relativeVelocity;
        }
    }

    private void CreateOrbitPath()
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

    private void UpdateOrbitWidth()
    {
        var lineRenderer = this.GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            return;
        }
        lineRenderer.startWidth = lineRenderer.endWidth = this.constants.OrbitLineWidth;
    }
}
