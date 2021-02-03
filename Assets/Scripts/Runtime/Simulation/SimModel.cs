// unset

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class SimModel
{
    public class SphereOfInfluence
    {
        public GravitySource g;
        public float maxForce;

        public Vector3 maxForcePosition;
        public int maxForceTick;

        public int startTick;
        public int endTick;

        public int duration => this.endTick - this.startTick;

        public SphereOfInfluence(GravitySource g, int startTick)
        {
            this.g = g;
            this.startTick = this.endTick = startTick;
        }
    }

    private struct SimOrbit
    {
        public Orbit from;
        public int parent;
        public OrbitParameters.OrbitPath orbit;
    }

    private struct SimGravity
    {
        public int fromId;
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
    private List<SimOrbit> simOrbits;

    // Gravity parameters of all simulated bodies
    private List<SimGravity> simGravitySources;

    #region SimState
    // Represents the current state of a simulation
    private class SimState
    {
        public PathSection path;
        public List<PathSection> relativePaths;

        //public readonly List<Vector3> path = new List<Vector3>();
        public Vector3 velocity;
        //public float pathLength = 0;
        public int crashTick = -1;
        public Vector3 crashPosition;

        public bool crashed => this.crashTick != -1;

        // A consecutive list of spheres of influence the path passes through.
        // It can refer to the same GravitySource more than once.
        public readonly List<SphereOfInfluence> sois = new List<SphereOfInfluence>();
        private int soiLastGId = -1;

        private readonly SimModel owner;
        private readonly float collisionRadius;
        private readonly float gravitationalConstant;

        private readonly float gravitationalRescaling;
        //readonly float startTime;
        //readonly float dt;

        private Vector3 position;
        private readonly float dt;
        private readonly int startTick;
        private readonly int maxTicks;
        private readonly int tickStep;
        private int tick;

        private float stepDt => this.dt * this.tickStep;
        private float time => this.dt * this.tick;

        public SimState(SimModel owner, Vector3 startPosition, Vector3 startVelocity, int startTick,
            float collisionRadius, float gravitationalConstant, float gravitationalRescaling, float dt, int maxTicks, int tickStep = 8)
        {
            this.owner = owner;
            this.collisionRadius = collisionRadius;
            this.gravitationalConstant = gravitationalConstant;
            this.gravitationalRescaling = gravitationalRescaling;
            this.position = startPosition;
            this.dt = dt;
            this.tick = this.startTick = startTick;
            this.maxTicks = maxTicks;
            this.tickStep = tickStep;
            this.velocity = startVelocity;
            this.path = new PathSection(startTick, tickStep);
            this.relativePaths = owner.simGravitySources.Select(_ => new PathSection(startTick, tickStep)).ToList();
        }
        
        public bool Step()
        {
            var forceInfo = this.owner.CalculateForce(this.time, this.position, this.gravitationalConstant, this.gravitationalRescaling);

            if (forceInfo.valid)
            {
                // Determine which gravity source imparts the highest force 
                int maxIndex = 0;
                float maxForce = 0;
                var maxForcePosition = Vector3.zero;
                for (int i = 0; i < forceInfo.forces.Length; i++)
                {
                    float forceMag = forceInfo.forces[i].magnitude;
                    if (forceMag > maxForce)
                    {
                        maxIndex = i;
                        maxForce = forceMag;
                        maxForcePosition = forceInfo.positions[i];
                    }
                }

                var maxSimGravitySource = this.owner.simGravitySources[maxIndex];
                var lastSoi = this.sois.LastOrDefault();
                if (lastSoi == null || this.soiLastGId != maxSimGravitySource.fromId)
                {
                    lastSoi = new SphereOfInfluence(maxSimGravitySource.from, this.tick);
                    this.soiLastGId = maxSimGravitySource.fromId;
                    this.sois.Add(lastSoi);
                }
                if (lastSoi.maxForce < maxForce)
                {
                    lastSoi.maxForce = maxForce;
                    lastSoi.maxForceTick = this.tick;
                    lastSoi.maxForcePosition = maxForcePosition;
                }
                lastSoi.endTick = this.tick;

                if (this.path.positions.Count > 0)
                {
                    // Detect if we will crash between the last step and this one
                    var collision = this.owner.DetectCrash(forceInfo, this.path.finalPosition, this.position - this.path.finalPosition,
                        this.collisionRadius);
                    if (collision.occurred)
                    {
                        //this.position = collision.at;
                        this.crashTick = Mathf.FloorToInt(this.tick - this.tickStep * (1 - collision.t));
                        this.crashPosition = collision.at;
                    }
                }

                // Update the paths
                for (int i = 0; i < forceInfo.positions.Length; i++)
                {
                    this.relativePaths[i].Add(this.position - forceInfo.positions[i], this.velocity);
                    //this.relativePaths[i].willCrash = this.crashed;
                }
                this.path.Add(this.position, this.velocity);
                //this.path.willCrash = this.crashed;
                
                // We update velocity and position after storing the current values into the paths as the paths need to start from
                // the provided start position and velocity. We can't just store them in the constructor because we need the 
                // forceInfo above to update the relative paths.
                //if (!this.crashed)
                //{
                this.velocity += forceInfo.rescaledTotalForce * this.stepDt;
                this.position += this.velocity * this.stepDt;
                //}
            }

            this.tick += this.tickStep;
            return !this.crashed && this.tick - this.startTick < this.maxTicks;
        }
    }
    #endregion

    public void DelayedInit()
    {
        if(this.orbits != null)
        {
            return;
        }

        var allOrbits = GameObject.FindObjectsOfType<Orbit>().Where(o => o.isActiveAndEnabled).ToList();
        this.orbits = new List<Orbit>();
        var orbitStack = new Stack<Orbit>(allOrbits.Where(o => o.gameObject.GetComponentInParentOnly<Orbit>() == null));
        while(orbitStack.Any())
        {
            var orbit = orbitStack.Pop();
            this.orbits.Add(orbit);
            var directChildren = allOrbits.Where(o => o.gameObject.GetComponentInParentOnly<Orbit>() == orbit).Reverse().ToList();
            // this.orbits.AddRange(directChildren);
            foreach (var child in directChildren)
            {
                orbitStack.Push(child);
            }
        }

        // Orbits ordered in depth first search ordering, with parent indices
        this.simOrbits = this.orbits.Select(
            o => new SimOrbit {
                @from = o,
                orbit = o.orbitPath,
                parent = this.orbits.IndexOf(o.gameObject.GetComponentInParentOnly<Orbit>())
            }).ToList();

        var invalidOrbits = this.simOrbits.Where(s => s.from.position.position.z != 0f);
        Debug.Assert(!invalidOrbits.Any(), $"All orbits must be at z = 0, {string.Join(", ", invalidOrbits.Select(s => s.from.gameObject.name))}!");

        // NOTE: We assume that if the gravity source has a parent orbit then its local position is 0, 0, 0.
        var allGravitySources = GravitySource.All();

        // Gravity sources with parent orbits (if the have one), and global positions (in case they don't).
        this.simGravitySources = allGravitySources
            .Select(g =>
            {
                var parentGravitySource = g.gameObject.GetComponentInParentOnly<GravitySource>();
                return new SimGravity
                {
                    @from = g,
                    fromId = g.GetInstanceID(), // we assign the reference here, because we want a stable comparable handle to g that we can use outside the main thread
                    mass = g.parameters.mass, // we only need the mass, density is only required to calculate mass initially
                    radius = g.radius, // radius is applied using local scale on the same game object as the gravity
                    orbitIndex = this.orbits.IndexOf(g.gameObject.GetComponentInParent<Orbit>()),
                    gravityParentIndex = allGravitySources.FindIndex(s => s == parentGravitySource),
                    position = g.position
                };
            }).ToList();

        var invalidGravitySources = this.simGravitySources.Where(s => s.position.z != 0f);
        Debug.Assert(!invalidGravitySources.Any(), $"All gravity sources must be at z = 0, {string.Join(", ", invalidGravitySources.Select(s => s.from.gameObject.name))}!");
    }

    // OPT: If required we could preallocate this and pass it as a parameter instead
    public struct ForceInfo
    {
        // Position of each gravity source
        public Vector3[] positions;
        // Velocity of each gravity source
        public Vector3[] velocities;
        // Force from each gravity source
        public Vector3[] forces;
        // Total force
        public Vector3 totalForce;
        // Total force with gravity rescaling applied
        public Vector3 rescaledTotalForce;
        // Index of the current primary
        public int primaryIndex;

        public bool valid => this.forces != null && this.positions != null && this.velocities != null;
    }

    public ForceInfo CalculateForce(float time, Vector3 position, float gravitationalConstant, float gravitationalRescaling)
    {
        this.DelayedInit();
        if(!this.simGravitySources.Any())
        {
            return new ForceInfo {};
        }

        var orbitPositions = new Vector3[this.simOrbits.Count];
        var orbitVelocities = new Vector3[this.simOrbits.Count];

        for (int i = 0; i < this.simOrbits.Count; i++)
        {
            var o = this.simOrbits[i];
            var (localPosition, localVelocity) = o.orbit.GetPositionVelocity(time);
            orbitPositions[i] = o.parent != -1 ? orbitPositions[o.parent] + localPosition : localPosition;
            orbitVelocities[i] = o.parent != -1 ? orbitVelocities[o.parent] + localVelocity : localVelocity;
        }

        // Calculate raw forces
        var positions = new Vector3[this.simGravitySources.Count];
        var velocities = new Vector3[this.simGravitySources.Count];
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
            velocities[i] = g.orbitIndex != -1 ? orbitVelocities[g.orbitIndex] : Vector3.zero;
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
        float[] rescaledForceMags = new float[this.simGravitySources.Count];
        for (int i = 0; i < this.simGravitySources.Count; i++)
        {
            totalForce += forces[i];
            var rescaledForce = !parents.Contains(i)
                ? forces[i].normalized * (maxForceMag * Mathf.Pow(forces[i].magnitude / maxForceMag, gravitationalRescaling))
                : forces[i];
            rescaledTotalForce += rescaledForce;
            float rescaledForceMag = rescaledForce.magnitude;
            rescaledForceMags[i] = rescaledForceMag;// < 0.01f ? 0 : rescaledForceMag;
        }

        return new ForceInfo {
            positions = positions,
            velocities = velocities,
            forces = forces,
            totalForce = totalForce,
            rescaledTotalForce = rescaledTotalForce,
            primaryIndex = primaryIndex
        };
    }

    public Geometry.Intersect DetectCrash(ForceInfo forceInfo, Vector3 previousPosition, Vector3 moveVector, float collisionRadius)
    {
        var dir = moveVector.normalized;
        float maxT = moveVector.magnitude;
        for (int i = 0; i < this.simGravitySources.Count; i++)
        {
            var g = this.simGravitySources[i];
            var planetPosition = forceInfo.positions[i];

            var collision = Geometry.IntersectRaySphere(previousPosition, dir, planetPosition, collisionRadius + g.radius);
            if (collision.occurred && collision.t <= maxT)
            {
                collision.t /= maxT;
                return collision;
            }
        }

        return Geometry.Intersect.none;
    }

    private static int pathsBeingCalculated = 0;
    
    public async Task<SimPath> CalculateSimPathAsync(Vector3 position, Vector3 velocity, int startTick, float timeStep, int ticks, float collisionRadius, float gravitationalConstant, float gravitationalRescaling)
    {
        this.DelayedInit();

        var state = new SimState(
            owner: this,
            startPosition: position,
            startVelocity: velocity,
            startTick: startTick,
            collisionRadius: collisionRadius,
            gravitationalConstant: gravitationalConstant,
            gravitationalRescaling: gravitationalRescaling,
            dt: timeStep,
            maxTicks: ticks,
            tickStep: 32
        );

#if UNITY_WEBGL
        SimModel.pathsBeingCalculated++;
        try
        {
            var stopwatch = Stopwatch.StartNew();
            bool cont = true;
            while (cont)
            {
                // Do 20 ticks at a time
                for (int i = 0; cont && i < 20; ++i)
                {
                    cont = state.Step();
                }
                if (cont && stopwatch.ElapsedMilliseconds > 10 / SimModel.pathsBeingCalculated)
                {
                    await Awaiters.NextFrame;
                    stopwatch.Restart();
                }
            }
        }
        finally
        {
            SimModel.pathsBeingCalculated--;
        }
#else
        // Hand off to another thread
        await Task.Run(() =>
        {
            while (state.Step()) { }
        });
#endif

        var relativePaths = new Dictionary<GravitySource, PathSection>();
        for (int i = 0; i < this.simGravitySources.Count; i++)
        {
            relativePaths[this.simGravitySources[i].from] = state.relativePaths[i];
        }
        return new SimPath
        { 
            pathSection = state.path,
            relativePaths = relativePaths,
            sois = state.sois,
            crashTick = state.crashTick,
            crashPosition = state.crashPosition,
        };
    }
}