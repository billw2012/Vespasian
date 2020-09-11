using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;
using static SimModel;

public class SimManager : MonoBehaviour
{
    public GameConstants constants;

    [HideInInspector]
    public float simTime;

    SimMovement[] simulatedObjects;

    readonly SimModel model = new SimModel();

    void OnValidate()
    {
        Assert.IsNotNull(this.constants);
    }

    void Start()
    {
        Assert.IsNotNull(this.constants);

        this.simulatedObjects = FindObjectsOfType<SimMovement>().ToArray();
    }

    void FixedUpdate()
    {
        this.model.DelayedInit();

        float dt = Time.fixedDeltaTime * this.constants.GameSpeedBase;
        this.simTime += dt;

        // Update game objects from model (we use the simModels orbit list so we keep consistent ordering)
        foreach (var o in this.model.orbits)
        {
            if (o != null)
                o.SimUpdate(this.simTime);
        }

        foreach (var s in this.simulatedObjects.Where(s => s != null).Where(s => s.gameObject.activeInHierarchy && s.isActiveAndEnabled))
        {
            s.SimUpdate(this.simTime);
        }
    }

    public SectionedSimPath CreateSectionedSimPath(Vector3 startPosition, Vector3 startVelocity, float targetLength, float proximityWarningRange, int sectionSteps = 100)
    {
        return new SectionedSimPath(this.model, this.simTime, startPosition, startVelocity, targetLength, Time.fixedDeltaTime, this.constants.GravitationalConstant, this.constants.GravitationalRescaling, proximityWarningRange, sectionSteps);
    }
}

public class SimPath
{
    public List<Vector3> path;
    public List<SphereOfInfluence> sois;
    public float dt;
    public float timeStart;
    public Vector3 finalVelocity;
    public bool crashed;

    public float duration => this.dt * this.path.Count;
    public float timeEnd => this.timeStart + this.duration;
    public Vector3 finalPosition => this.path[this.path.Count - 1];

    public bool InRange(float time) => time >= this.timeStart && time <= this.timeEnd && this.path != null && this.path.Count >= 4;

    public (Vector3, Vector3) GetPositionVelocity(float t)
    {
        Assert.IsTrue(this.InRange(t));

        float fIdx = (t - this.timeStart) / this.dt;

        int idx0 = Mathf.Clamp(Mathf.FloorToInt(fIdx), 0, this.path.Count - 1);
        int idx1 = Mathf.Clamp(idx0 + 1, 0, this.path.Count - 1);
        float frac = fIdx - Mathf.FloorToInt(fIdx);
        var position = Vector3.Lerp(this.path[idx0], this.path[idx1], frac);

        var velocity = idx1 + 1 > this.path.Count - 1 ?
            (position - Vector3.Lerp(this.path[idx0 - 1], this.path[idx0], frac)) / this.dt
            :
            (Vector3.Lerp(this.path[idx1], this.path[idx1 + 1], frac) - position) / this.dt;
        return (position, velocity);
    }

    public void TrimStart(float beforeTime)
    {
        int count = Mathf.Clamp(Mathf.FloorToInt((beforeTime - this.timeStart) / this.dt), 0, this.path.Count);
        this.path.RemoveRange(0, count);
        this.timeStart += count * this.dt;
        this.sois = this.sois.Where(s => s.endTime > this.timeStart).ToList();
    }

    public void Append(SimPath other)
    {
        Assert.IsFalse(this.crashed);
        Assert.IsTrue(Mathf.Approximately(other.timeStart, this.timeEnd));
        Assert.IsTrue(Mathf.Approximately(other.dt, this.dt));

        this.crashed = other.crashed;
        this.path.AddRange(other.path);
        this.sois.AddRange(other.sois);
        this.finalVelocity = other.finalVelocity;
    }
}

public class SimModel
{
    public class SphereOfInfluence
    {
        public GravitySource g;
        public float maxForce;
        public float startTime;
        public float endTime;
    }

    struct SimOrbit
    {
        public Orbit from;
        public int parent;
        public OrbitParameters.OrbitPath orbit;
    }

    struct SimGravity
    {
        public GravitySource from;
        public int orbitIndex;
        public int gravityParentIndex;
        public float mass;
        public float radius;
        public Vector3 position;
    }

    // Real Orbits for updating
    public List<Orbit> orbits;

    // Orbit parameters of all simulated bodies
    List<SimOrbit> simOrbits;

    // Gravity parameters of all simulated bodies
    List<SimGravity> simGravitySources;

    #region SimState
    // Represents the current state of a simulation
    class SimState
    {
        public readonly List<Vector3> path = new List<Vector3>();
        public Vector3 velocity;
        public float pathLength = 0;
        public bool crashed;
        // A consecutive list of spheres of influence the path passes through.
        // It can refer to the same GravitySource more than once.
        public readonly List<SphereOfInfluence> sois = new List<SphereOfInfluence>();

        readonly SimModel owner;
        readonly float collisionRadius;
        readonly float gravitationalConstant;
        readonly float gravitationalRescaling;
        readonly float startTime;

        Vector3 position;
        float time;

        public SimState(SimModel owner, Vector3 startPosition, Vector3 startVelocity, float startTime, float collisionRadius, float gravitationalConstant, float gravitationalRescaling)
        {
            this.owner = owner;
            this.collisionRadius = collisionRadius;
            this.gravitationalConstant = gravitationalConstant;
            this.gravitationalRescaling = gravitationalRescaling;
            this.position = startPosition;
            this.velocity = startVelocity;
            this.time = this.startTime = startTime;
        }

        public void Step(float dt)
        {
            this.time += dt;

            var forceInfo = this.owner.CalculateForce(this.time, this.position, this.gravitationalConstant, this.gravitationalRescaling);

            int maxIndex = 0;
            float maxForce = 0;
            for (int i = 0; i < forceInfo.forces.Length; i++)
            {
                float forceMag = forceInfo.forces[i].magnitude;
                if (forceMag > maxForce)
                {
                    maxIndex = i;
                    maxForce = forceMag;
                }
            }

            var bestG = this.owner.simGravitySources[maxIndex].from;
            if (this.sois.Count == 0)
            {
                this.sois.Add(new SphereOfInfluence { g = bestG, startTime = this.startTime } );
            }
            else
            {
                var lastSoi = this.sois.Last();
                if (lastSoi.g == bestG)
                {
                    lastSoi.maxForce = Mathf.Max(lastSoi.maxForce, maxForce);
                }
                else
                {
                    this.sois.Add(new SphereOfInfluence { g = bestG, startTime = this.time });
                }
            }
            this.sois.Last().endTime = this.time;


            this.velocity += forceInfo.rescaledTotalForce * dt;

            var oldPosition = this.position;
            this.position += this.velocity * dt;


            this.crashed = false;
            for (int i = 0; i < this.owner.simGravitySources.Count; i++)
            {
                var g = this.owner.simGravitySources[i];
                var planetPosition = forceInfo.positions[i];

                var collision = Geometry.IntersectRaySphere(oldPosition, this.velocity.normalized, planetPosition, this.collisionRadius + g.radius);
                if (collision.occurred && collision.t < this.velocity.magnitude * dt * 3)
                {
                    this.position = collision.at;
                    this.crashed = true;
                    break;
                }
            }
            this.path.Add(this.position);

            this.pathLength += Vector3.Distance(oldPosition, this.position);
        }
    }
    #endregion

    public void DelayedInit()
    {
        if(this.orbits != null)
        {
            return;
        }

        var allOrbits = GameObject.FindObjectsOfType<Orbit>().Where(o => o.isActiveAndEnabled);
        this.orbits = new List<Orbit>();
        var orbitStack = new Stack<Orbit>(allOrbits.Where(o => o.gameObject.GetComponentInParentOnly<Orbit>() == null));
        while(orbitStack.Any())
        {
            var orbit = orbitStack.Pop();
            this.orbits.Add(orbit);
            var directChildren = allOrbits.Where(o => o.gameObject.GetComponentInParentOnly<Orbit>() == orbit);
            this.orbits.AddRange(directChildren.Reverse());
        }

        // Orbits ordered in depth first search ordering, with parent indices
        this.simOrbits = this.orbits.Select(
            o => new SimOrbit {
                from = o,
                orbit = o.orbitPath,
                parent = this.orbits.IndexOf(o.gameObject.GetComponentInParentOnly<Orbit>())
            }).ToList();

        // NOTE: We assume that if the gravity source has a parent orbit then its local position is 0, 0, 0.
        var allGravitySources = GravitySource.All();

        // Gravity sources with parent orbits (if the have one), and global positions (in case they don't).
        this.simGravitySources = allGravitySources
            .Select(g =>
            {
                var parentGravitySource = g.gameObject.GetComponentInParentOnly<GravitySource>();
                return new SimGravity
                {
                    from = g,
                    mass = g.parameters.mass, // we only need the mass, density is only required to calculate mass initially
                    radius = g.radius, // radius is applied using local scale on the same game object as the gravity
                    orbitIndex = this.orbits.IndexOf(g.gameObject.GetComponentInParent<Orbit>()),
                    gravityParentIndex = allGravitySources.FindIndex(s => s == parentGravitySource),
                    position = g.position
                };
            }).ToList();
    }

    // OPT: If required we could preallocate this and pass it as a parameter instead
    public struct ForceInfo
    {
        // Position of each gravity source
        public Vector3[] positions;
        // Force from each gravity source
        public Vector3[] forces;
        // Total force
        public Vector3 totalForce;
        // Total force with gravity rescaling applied
        public Vector3 rescaledTotalForce;
    }

    public ForceInfo CalculateForce(float time, Vector3 position, float gravitationalConstant, float gravitationalRescaling)
    {
        this.DelayedInit();

        var orbitPositions = new Vector3[this.simOrbits.Count];

        for (int i = 0; i < this.simOrbits.Count; i++)
        {
            var o = this.simOrbits[i];
            var localPosition = o.orbit.GetPosition(time);
            orbitPositions[i] = o.parent != -1 ? orbitPositions[o.parent] + localPosition : localPosition;
        }

        // Calculate raw forces
        var positions = new Vector3[this.simGravitySources.Count];
        var forces = new Vector3[this.simGravitySources.Count];
        int primaryIndex = 0;

        for (int i = 0; i < this.simGravitySources.Count; i++)
        {
            var g = this.simGravitySources[i];
            var gPosition = g.orbitIndex != -1 ? orbitPositions[g.orbitIndex] : g.position;
            var force = OrbitalUtils.CalculateForce(
                gPosition - position,
                g.mass,
                gravitationalConstant);
            positions[i] = gPosition;
            forces[i] = force;
            if(force.magnitude > forces[primaryIndex].magnitude)
            {
                primaryIndex = i;
            }
        }

        // We try to reduce perturbation by reducing the force of bodies that are outside
        // of the current primary gravity sources parent hierarchy

        // Determine all non-parents of the primary
        var parents = new List<int>();
        int currParent = this.simGravitySources[primaryIndex].gravityParentIndex;
        while (currParent != -1)
        {
            parents.Add(currParent);
            currParent = this.simGravitySources[currParent].gravityParentIndex;
        }

        // Apply force rescaling
        var totalForce = Vector3.zero;
        var rescaledTotalForce = Vector3.zero;
        float maxForceMag = forces[primaryIndex].magnitude;
        for (int i = 0; i < this.simGravitySources.Count; i++)
        {
            totalForce += forces[i];
            if(!parents.Contains(i))
            {
                rescaledTotalForce += forces[i].normalized * maxForceMag * Mathf.Pow(forces[i].magnitude / maxForceMag, gravitationalRescaling);
            }
            else
            {
                rescaledTotalForce += forces[i];
            }
        }

        return new ForceInfo {
            positions = positions,
            forces = forces,
            totalForce = totalForce,
            rescaledTotalForce = rescaledTotalForce
        };
    }

    public async Task<SimPath> CalculateSimPathAsync(Vector3 position, Vector3 velocity, float startTime, float timeStep, int steps, float collisionRadius, float gravitationalConstant, float gravitationalRescaling)
    {
        this.DelayedInit();

        var state = new SimState(
            owner: this,
            startPosition: position,
            startVelocity: velocity,
            startTime: startTime,
            collisionRadius: collisionRadius,
            gravitationalConstant: gravitationalConstant,
            gravitationalRescaling: gravitationalRescaling
        );

        // Hand off to another thread
        await Task.Run(() =>
        {
            for (int i = 0; i < steps && !state.crashed; i++)
            {
                state.Step(timeStep);
            }
        });

        return new SimPath { 
            dt = timeStep,
            finalVelocity = state.velocity,
            path = state.path,
            timeStart = startTime,
            sois = state.sois,
            crashed = state.crashed
        };
    }
}

public class SectionedSimPath
{
    public Vector3 position;
    public Vector3 velocity;
    public bool crashed => this.path?.crashed == true;

    readonly SimModel model;
    readonly float targetLength;
    readonly int sectionSteps;
    readonly float dt;
    readonly float gravitationalConstant;
    readonly float gravitationalRescaling;
    readonly float proximityWarningRange;

    // List<SimPath> pathSections = new List<SimPath>();
    SimPath path;
    float simTime;
    bool sectionIsQueued = false;
    //SimPath lastValidPathSection = null;
    bool restartPath = true;

    public SectionedSimPath(SimModel model, float startSimTime, Vector3 startPosition, Vector3 startVelocity, float targetLength, float dt, float gravitationalConstant, float gravitationalRescaling, float proximityWarningRange, int sectionSteps = 100)
    {
        this.model = model;
        this.targetLength = targetLength;
        this.sectionSteps = sectionSteps;
        this.dt = dt;
        this.gravitationalConstant = gravitationalConstant;
        this.gravitationalRescaling = gravitationalRescaling;
        this.proximityWarningRange = proximityWarningRange;

        this.position = startPosition;
        this.velocity = startVelocity;
        this.simTime = startSimTime;
    }

    public IEnumerable<Vector3> GetFullPath()
    {
        return this.path?.path ?? new List<Vector3>();
    }

    public IEnumerable<SphereOfInfluence> GetFullPathSOIs()
    {
        return this.path?.sois ?? new List<SphereOfInfluence>();
    }

    public void Step(float simTime, Vector3 force)
    {
        this.simTime = simTime;

        //// Remove old sections
        //while (this.pathSections.Count > 0 && this.pathSections[0].timeEnd <= this.simTime)
        //{
        //    this.pathSections.RemoveAt(0);
        //}

        this.path?.TrimStart(this.simTime);

        var prevVel = this.velocity;
        // Get new position, either from the path or via dead reckoning
        if (this.path != null && this.path.InRange(this.simTime) && force.magnitude == 0)
        {
            (this.position, this.velocity) = this.path.GetPositionVelocity(this.simTime);
        }
        else
        {
            // Hopefully we will never hit this for more than a frame
            this.velocity += this.model.CalculateForce(this.simTime, this.position, this.gravitationalConstant, this.gravitationalRescaling).rescaledTotalForce * this.dt;
            this.velocity += force * this.dt;
            this.position += this.velocity * this.dt;
            this.restartPath = true;
            // Start recreating the path sections
        }

        if((prevVel - this.velocity).magnitude > 1)
        {
            Debug.DebugBreak();
        }

        if (!this.sectionIsQueued && 
            (this.restartPath ||
            this.path == null ||
            (this.path.duration < this.targetLength && !this.crashed)))
        {
            this.GenerateNewSection();
        }
    }

    // float GetTotalPathDuration() => this.path?..Select(p => p.duration).Sum();

    async void GenerateNewSection()
    {
        this.sectionIsQueued = true;

        // TODO? Might need to avoid restarting the path until we calculated all the required sections of the old path
        //       In this case perhaps it makes no sense to calculate it in sections at all? Maybe for aesthetics and less laggy feeling,
        //       but we could still interpolate the whole path anyway...
        if (this.restartPath || this.path == null)
        {
            this.restartPath = false; // Reset this to allow it to be set again immediately if required
            this.path = await this.model.CalculateSimPathAsync(this.position, this.velocity, this.simTime, this.dt, (int)(this.targetLength / this.dt), this.proximityWarningRange, this.gravitationalConstant, this.gravitationalRescaling);
        }
        else //if(this.path.duration < this.targetLength)
        {
            this.path.Append(await this.model.CalculateSimPathAsync(this.path.finalPosition, this.path.finalVelocity, this.path.timeEnd, this.dt, this.sectionSteps, this.proximityWarningRange, this.gravitationalConstant, this.gravitationalRescaling));
        }

        this.sectionIsQueued = false;
    }
}
