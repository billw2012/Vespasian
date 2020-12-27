using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;
using static SimModel;


public interface ISimUpdate
{
    void SimUpdate(int simTick, int timeStep);
    void SimRefresh();
}

/// <summary>
/// All our custom simulation is updated by this class.
/// It allows for time warping (by integer multipliers only).
/// It ensures order of update is strict.
/// </summary>
public class Simulation : MonoBehaviour
{
    public GameConstants constants;

    public int timeStep { get; set; } = 1;

    public int simTick { get; set; } = 0;

    public float time => this.simTick * Time.fixedDeltaTime;

    public float dt => this.timeStep * Time.fixedDeltaTime;

    private List<MonoBehaviour> simulatedObjects;
    // SimMovement[] simulatedObjects;

    private SimModel model;

    private void OnValidate()
    {
        Assert.IsNotNull(this.constants);
    }

    private void Start()
    {
        Assert.IsNotNull(this.constants);
        this.Refresh();
    }

    private void FixedUpdate()
    {
        this.model.DelayedInit();

        this.simTick += this.timeStep;

        // Update game objects from model (we use the simModels orbit list so we keep consistent ordering)
        foreach (var o in this.model.orbits
            .Where(o => o != null))
        {
            o.SimUpdate(this.simTick);
        }

        foreach (var s in this.simulatedObjects
            .Where(s => s != null)
            .Where(s => s.gameObject.activeInHierarchy && s.isActiveAndEnabled)
            .OfType<ISimUpdate>())
        {
            s.SimUpdate(this.simTick, this.timeStep);
        }
    }

    public void Refresh()
    {
        this.model = new SimModel();
        this.model.DelayedInit();
        this.simulatedObjects = FindObjectsOfType<MonoBehaviour>().OfType<ISimUpdate>().OfType<MonoBehaviour>().ToList();
        foreach (var s in this.simulatedObjects.OfType<ISimUpdate>())
        {
            s.SimRefresh();
        }
    }

    public SectionedSimPath CreateSectionedSimPath(Vector3 startPosition, Vector3 startVelocity, int targetTicks, float proximityWarningRange, int sectionTicks = 200)
    {
        return new SectionedSimPath(this.model, this.simTick, startPosition, startVelocity, targetTicks, Time.fixedDeltaTime, this.constants.GravitationalConstant, this.constants.GravitationalRescaling, proximityWarningRange, sectionTicks);
    }
}

public class PathSection
{
    // private struct Key
    // {
    //     public int tick;
    //     public Vector3 position;
    // }
    public readonly List<Vector3> positions;
    public readonly List<Vector3> velocities;
    public readonly int tickStep;
    public int startTick;

    // A single position has duration of 0, so we adjust positions.Count here to reflect that
    public int durationTicks => Mathf.Max(0, this.positions.Count - 1) * this.tickStep;
    public int endTick => this.startTick + this.durationTicks;
    public Vector3 finalVelocity => this.velocities[this.velocities.Count - 1];
    public Vector3 finalPosition => this.positions[this.positions.Count - 1];

    public bool InRange(float tick) => tick >= this.startTick && tick <= this.endTick && this.positions != null && this.positions.Count >= 4;

    public PathSection(int startTick, int tickStep)
    {
        // Assert.IsTrue(Mathf.IsPowerOfTwo(tickStep));
        this.startTick = startTick;
        this.tickStep = tickStep;
        this.positions = new List<Vector3>();
        this.velocities = new List<Vector3>();
    }

    public (Vector3, Vector3) GetPositionVelocity(float tick, float dt)
    {
        Assert.IsTrue(this.InRange(tick));

        float fIdx = (tick - this.startTick) / this.tickStep;

        int idx0 = Mathf.Clamp(Mathf.FloorToInt(fIdx), 0, this.positions.Count - 1);
        int idx1 = Mathf.Clamp(idx0 + 1, 0, this.positions.Count - 1);
        float t = fIdx - Mathf.FloorToInt(fIdx);
        //var position = Vector3.Lerp(this.positions[idx0], this.positions[idx1], frac);

        // var velocity = idx1 + 1 > this.positions.Count - 1 ?
        //     position - Vector3.Lerp(this.positions[idx0 - 1], this.positions[idx0], frac)
        //     :
        //     Vector3.Lerp(this.positions[idx1], this.positions[idx1 + 1], frac) - position;
        // return (position.xy0(), velocity.xy0() / (dt * this.tickStep));
        return (
            Vector3.Lerp(this.positions[idx0], this.positions[idx1], t).xy0(),
            Vector3.Lerp(this.velocities[idx0], this.velocities[idx1], t).xy0()
            );
    }
    
    // https://en.wikipedia.org/wiki/Cubic_Hermite_spline#Interpolation_on_a_single_interval
    public (Vector3, Vector3) GetPositionVelocityHermite(float tick, float dt)
    {
        Assert.IsTrue(this.InRange(tick));

        float fIdx = (tick - this.startTick) / this.tickStep;

        int idx0 = Mathf.Clamp(Mathf.FloorToInt(fIdx), 0, this.positions.Count - 1);
        int idx1 = Mathf.Clamp(idx0 + 1, 0, this.positions.Count - 1);

        float t = fIdx - Mathf.FloorToInt(fIdx);

        return (
            MathX.Hermite(
                this.positions[idx0],
                this.velocities[idx0] * (this.tickStep * dt), 
                this.positions[idx1],
                this.velocities[idx1] * (this.tickStep * dt),
                t).xy0(),
            Vector3.Lerp(this.velocities[idx0], this.velocities[idx1], t).xy0()
        );
    }
    
    public void Add(Vector3 pos, Vector3 velocity)
    {
        this.positions.Add(pos.xy0());
        this.velocities.Add(velocity.xy0());
        //this.finalVelocity = velocity.xy0();
    }
    
    public void TrimStart(int beforeTick)
    {
        int count = Mathf.Clamp((beforeTick - this.startTick) / this.tickStep, 0, this.positions.Count);
        this.positions.RemoveRange(0, count);
        this.velocities.RemoveRange(0, count);        
        this.startTick += count * this.tickStep;
    }

    public void Append(PathSection other)
    {
        Assert.AreEqual(other.startTick, this.endTick);
        Assert.AreEqual(other.tickStep, this.tickStep);
        Assert.AreEqual(other.positions.First(), this.positions.Last());
        Assert.AreEqual(other.velocities.First(), this.velocities.Last());
        
        // Skip the first position as it will be identical
        this.positions.AddRange(other.positions.Skip(1));
        this.velocities.AddRange(other.velocities.Skip(1));
        //this.finalVelocity = other.finalVelocity;
    }
}

public class SimPath
{
    public PathSection pathSection;
    public Dictionary<GravitySource, PathSection> relativePaths;
    public List<SphereOfInfluence> sois;
    public bool crashed;
    
    public void TrimStart(int beforeTick)
    {
        this.pathSection.TrimStart(beforeTick);
        foreach(var r in this.relativePaths.Values)
        {
            r.TrimStart(beforeTick);
        }
        this.sois = this.sois.Where(s => s.endTick > this.pathSection.startTick).ToList();
        // this.sois.FirstOrDefault()?.relativePath.TrimStart(beforeTick);
    }

    public void Append(SimPath other)
    {
        Assert.IsFalse(this.crashed);

        this.crashed = other.crashed;
        this.pathSection.Append(other.pathSection);
        foreach(var gp in other.relativePaths)
        {
            this.relativePaths[gp.Key].Append(gp.Value);
        }

        // If we have sois to merge
        if (this.sois.Any() && other.sois.Any() && this.sois.Last().g == other.sois.First().g)
        {
            var ourLastSoi = this.sois.Last();
            var otherFirstSoi = other.sois.First();
            if (otherFirstSoi.maxForce > ourLastSoi.maxForce)
            {
                ourLastSoi.maxForce = otherFirstSoi.maxForce;
                ourLastSoi.maxForcePosition = otherFirstSoi.maxForcePosition;
                ourLastSoi.maxForceTick = otherFirstSoi.maxForceTick;
            }
            ourLastSoi.endTick = otherFirstSoi.endTick;
            // ourLastSoi.relativePath.Append(otherFirstSoi.relativePath);
            // We merged the first soi of the others so we skip it and append the remaining ones
            this.sois.AddRange(other.sois.Skip(1));
        }
        else
        {
            // This covers all other cases
            this.sois.AddRange(other.sois);
        }
    }
}

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
        public bool crashed;
        // A consecutive list of spheres of influence the path passes through.
        // It can refer to the same GravitySource more than once.
        public readonly List<SphereOfInfluence> sois = new List<SphereOfInfluence>();

        private readonly SimModel owner;
        private readonly float collisionRadius;
        private readonly float gravitationalConstant;

        private readonly float gravitationalRescaling;
        //readonly float startTime;
        //readonly float dt;

        private Vector3 position;
        private readonly float dt;
        private int startTick;
        private int tick;
        private int maxTicks;
        private int tickStep;
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
            //owner.simGravitySources.ToDictionary(g => g.from, g => new PathSection(startTick));
        }

        // public void CompleteStep()
        // {
        //     throw new NotImplementedException();
        // }

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

                var bestG = this.owner.simGravitySources[maxIndex].from;
                var lastSoi = this.sois.LastOrDefault();
                if (lastSoi?.g != bestG)
                {
                    lastSoi = new SphereOfInfluence(bestG, this.tick);
                    this.sois.Add(lastSoi);
                }
                if (lastSoi.maxForce < maxForce)
                {
                    lastSoi.maxForce = maxForce;
                    lastSoi.maxForceTick = this.tick;
                    lastSoi.maxForcePosition = maxForcePosition;
                }
                lastSoi.endTick = this.tick;
                
                // Detect if we will crash on this tick step
                this.crashed = false;
                for (int i = 0; i < this.owner.simGravitySources.Count; i++)
                {
                    var g = this.owner.simGravitySources[i];
                    var planetPosition = forceInfo.positions[i];

                    var collision = Geometry.IntersectRaySphere(this.position, this.velocity.normalized, planetPosition, this.collisionRadius + g.radius);
                    if (collision.occurred && collision.t < this.velocity.magnitude * this.stepDt * 3)
                    {
                        this.position = collision.at;
                        this.crashed = true;
                        break;
                    }
                }

                // Update the paths
                for (int i = 0; i < forceInfo.positions.Length; i++)
                {
                    this.relativePaths[i].Add(this.position - forceInfo.positions[i], this.velocity);
                }
                this.path.Add(this.position, this.velocity);
                
                // We update velocity and position after storing the current values into the paths as the paths need to start from
                // the startTick, not the tick after
                if (!this.crashed)
                {
                    this.velocity += forceInfo.rescaledTotalForce * this.stepDt;
                    this.position += this.velocity * this.stepDt;
                }
            }

            this.tick += this.tickStep;
            return !this.crashed && this.tick - this.startTick < this.maxTicks;
            //this.pathLength += Vector3.Distance(oldPosition, this.position);
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
                from = o,
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
                    from = g,
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

        // Hand off to another thread
        await Task.Run(() =>
        {
            while (state.Step()) { }
        });

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
            crashed = state.crashed
        };
    }
}

public class SectionedSimPath
{
    public Vector3 position;
    public Vector3 velocity;
    public Vector3 relativeVelocity;
    public bool crashed => this.simPath?.crashed == true;

    private readonly SimModel model;
    private readonly int targetTicks;
    private readonly int sectionTicks;
    private readonly float dt;
    private readonly float gravitationalConstant;
    private readonly float gravitationalRescaling;
    private readonly float proximityWarningRange;

    private SimPath simPath;
    private int simTick;
    private bool sectionIsQueued = false;
    private bool restartPath = true;

    public SectionedSimPath(SimModel model, int startSimTick, Vector3 startPosition, Vector3 startVelocity, int targetTicks, float dt, float gravitationalConstant, float gravitationalRescaling, float proximityWarningRange, int sectionTicks = 200)
    {
        this.model = model;
        this.targetTicks = targetTicks;
        this.sectionTicks = sectionTicks;
        this.dt = dt;
        this.gravitationalConstant = gravitationalConstant;
        this.gravitationalRescaling = gravitationalRescaling;
        this.proximityWarningRange = proximityWarningRange;

        this.position = startPosition;
        this.velocity = startVelocity;
        this.simTick = startSimTick;
    }

    private static readonly List<Vector3> Empty = new List<Vector3>();

    public List<Vector3> GetAbsolutePath() => this.simPath?.pathSection.positions ?? Empty;

    public PathSection GetRelativePath(GravitySource g) => this.simPath?.relativePaths[g];

    public IEnumerable<SphereOfInfluence> GetFullPathSOIs() => this.simPath?.sois ?? new List<SphereOfInfluence>();

    public void Step(int tick, Vector3 force)
    {
        this.simTick = tick;

        this.simPath?.TrimStart(this.simTick);

        // Get new position, either from the path or via dead reckoning
        if (this.simPath != null && this.simPath.pathSection.InRange(this.simTick) && force.magnitude == 0)
        {
            (this.position, this.velocity) = this.simPath.pathSection.GetPositionVelocityHermite(this.simTick, this.dt);

            if (this.simPath.sois.Any())
            {
                var firstSoi = this.simPath.sois.First();
                var orbitComponent = firstSoi.g.GetComponent<Orbit>();
                this.relativeVelocity = orbitComponent != null ?
                    this.velocity - orbitComponent.absoluteVelocity
                    :
                    this.velocity;
            }
            else
            {
                this.relativeVelocity = this.velocity;
            }
        }
        else
        {
            // Hopefully we will never hit this for more than a frame
            var forceInfo = this.model.CalculateForce(this.simTick * this.dt, this.position, this.gravitationalConstant, this.gravitationalRescaling);
            if (forceInfo.valid)
            {
                this.velocity += forceInfo.rescaledTotalForce * this.dt;
            }
            this.velocity += force * this.dt;
            this.position += this.velocity * this.dt;
            this.relativeVelocity = forceInfo.valid 
                ? this.velocity - forceInfo.velocities[forceInfo.primaryIndex] 
                : this.velocity;

            // Start recreating the path sections
            this.restartPath = true;
        }
        
        Debug.DrawLine(this.position, this.position + this.velocity, Color.red);

        if (!this.sectionIsQueued && 
            (this.restartPath ||
            this.simPath == null ||
            this.simPath.pathSection.durationTicks < this.targetTicks && !this.crashed))
        {
            this.GenerateNewSection();
        }
    }

    // float GetTotalPathDuration() => this.path?..Select(p => p.duration).Sum();

    private async void GenerateNewSection()
    {
        this.sectionIsQueued = true;

        if (this.restartPath || this.simPath == null)
        {
            this.restartPath = false; // Reset this to allow it to be set again immediately if required
            this.simPath = await this.model.CalculateSimPathAsync(this.position, this.velocity, this.simTick, this.dt, this.targetTicks, this.proximityWarningRange, this.gravitationalConstant, this.gravitationalRescaling);
        }
        else
        {
            this.simPath.Append(await this.model.CalculateSimPathAsync(this.simPath.pathSection.finalPosition, this.simPath.pathSection.finalVelocity, this.simPath.pathSection.endTick, this.dt, this.sectionTicks, this.proximityWarningRange, this.gravitationalConstant, this.gravitationalRescaling));
        }

        this.sectionIsQueued = false;
    }
}
// SimUpdate should update orbit velocity on planets, then just use that to get relative velocity for the current soi instead of using GetPositionAndVelocity