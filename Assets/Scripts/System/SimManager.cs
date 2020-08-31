using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

public class SimManager : MonoBehaviour
{
    public GameConstants constants;

    //[Tooltip("Used to render the simulated path")]
    //public LineRenderer pathRenderer;

    //[Tooltip("Used to indicate a predicted crash")]
    //public GameObject warningSign;

    [HideInInspector]
    public float simTime;

    //[HideInInspector]
    //public List<SimModel.SphereOfInfluence> sois = new List<SimModel.SphereOfInfluence>();

    SimMovement[] simulatedObjects;
    //GameObject player;
    // Radius of the player
    //float radius;

    // Task representing the current instance of the player sim path update task
    //Task updatingPathTask = null;

    SimModel model = new SimModel();

    void OnValidate()
    {
        Assert.IsNotNull(this.constants);

        //if (this.pathRenderer != null)
        //{
        //    this.pathRenderer.positionCount = 0;
        //    this.pathRenderer.SetPositions(new Vector3[] { });
        //}
    }

    void Start()
    {
        Assert.IsNotNull(this.constants);

        //this.warningSign.SetActive(false);
        //this.player = FindObjectOfType<PlayerLogic>().gameObject;
        //Assert.IsNotNull(this.player);

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
            o.SimUpdate(this.simTime);
        }

        foreach (var s in this.simulatedObjects.Where(s => s.gameObject.activeInHierarchy && s.enabled))
        {
            s.SimUpdate(this.simTime, dt);
        }

        //this.pathRenderer.enabled = this.player.activeInHierarchy;
        //if (this.player.activeInHierarchy)
        //{
        //    this.UpdatePathAsync();

        //    this.UpdatePathWidth();
        //}
    }

    void DelayedInit()
    {
        //this.radius = this.player.GetComponentInChildren<MeshRenderer>().bounds.extents.x;
    }

    //async Task UpdatePath()
    //{
    //    var playerSimMovement = this.player.GetComponent<SimMovement>();

    //    // timeStep *must* match what the normal calculate uses
    //    var state = await this.simModel.CalculateSimState(
    //        playerSimMovement.simPosition,
    //        playerSimMovement.velocity,
    //        this.simTime,
    //        Time.fixedDeltaTime,
    //        this.constants.SimDistanceLimit,
    //        this.radius,
    //        this.constants.GravitationalConstant,
    //        this.constants.GravitationalRescaling);

    //    // Check Application.isPlaying to avoid the case where game was stopped before the task was finished (in editor).
    //    if (Application.isPlaying && this.pathRenderer != null && this.warningSign != null)
    //    {
    //        // Resume in main thread
    //        this.pathRenderer.positionCount = state.path.Count;
    //        this.pathRenderer.SetPositions(state.path.ToArray());
    //        this.sois = state.sois;
    //        //this.pathLength = state.pathLength;

    //        if (state.crashed && state.path.Count > 0)
    //        {
    //            var canvas = this.warningSign.GetComponent<Graphic>().canvas;
    //            if (canvas != null)
    //            {
    //                this.warningSign.SetActive(true);
    //                var rectTransform = this.warningSign.GetComponent<RectTransform>();
    //                var canvasSafeArea = canvas.ScreenToCanvasRect(Screen.safeArea);
    //                var targetCanvasPosition = canvas.WorldToCanvasPosition(state.path.Last());
    //                var clampArea = new Rect(
    //                    canvasSafeArea.x - rectTransform.rect.x,
    //                    canvasSafeArea.y - rectTransform.rect.y,
    //                    canvasSafeArea.width - rectTransform.rect.width,
    //                    canvasSafeArea.height - rectTransform.rect.height
    //                );
    //                rectTransform.anchoredPosition = clampArea.ClampToRectOnRay(targetCanvasPosition);
    //            }
    //        }
    //        else
    //        {
    //            this.warningSign.SetActive(false);
    //        }
    //    }
    //}

    //async void UpdatePathAsync()
    //{
    //    if (this.updatingPathTask == null)
    //    {
    //        this.updatingPathTask = this.UpdatePath();
    //        // Hand off to the other thread
    //        await this.updatingPathTask;
    //        // Back on the main thread
    //        this.updatingPathTask = null;
    //    }
    //}

    //void UpdatePathWidth()
    //{
    //    //this.pathRenderer.startWidth = 0;
    //    //this.pathRenderer.endWidth = (1 + 9 * this.pathLength / GameConstants.Instance.SimDistanceLimit);
    //    // Fixed width line in screen space:
    //    this.pathRenderer.startWidth = this.pathRenderer.endWidth = this.constants.SimLineWidth;
    //}
    public SectionedSimPath CreateSectionedSimPath(Vector3 startPosition, Vector3 startVelocity, float targetLength, int sectionSteps = 100)
    {
        return new SectionedSimPath(this.model, this.simTime, startPosition, startVelocity, targetLength, Time.fixedDeltaTime, this.constants.GravitationalConstant, this.constants.GravitationalRescaling, sectionSteps);
    }
}

public class SimPath
{
    public Vector3[] path;
    public float dt;
    public float timeStart;
    public float duration => this.dt * this.path.Length;
    public float timeEnd => this.timeStart + this.duration;
    public Vector3 finalPosition => this.path[this.path.Length - 1];
    public Vector3 finalVelocity;

    public Vector3 GetPosition(float t)
    {
        if (this.path == null || this.path.Length == 0)
        {
            return Vector3.zero;
        }

        float fIdx = (t - this.timeStart) / this.dt;

        int idx0 = Mathf.Clamp(Mathf.FloorToInt(fIdx), 0, this.path.Length - 1);
        int idx1 = Mathf.Clamp(idx0 + 1, 0, this.path.Length - 1);
        float frac = fIdx - Mathf.FloorToInt(fIdx);
        return Vector3.Lerp(this.path[idx0], this.path[idx1], frac);
    }
}

public class SimModel
{
    public class SphereOfInfluence
    {
        public GravitySource g;
        //public float radius;
        public float maxForce;
        //public float distance; // Distance when it was strongest SOI along the simulated path
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
        public int parent;
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

        readonly SimModel owner;
        readonly float collisionRadius;
        readonly float gravitationalConstant;
        readonly float gravitationalRescaling;
        Vector3 position;
        float time;

        // A consecutive list of spheres of influence the path passes through.
        // It can refer to the same GravitySource more than once.
        public readonly List<SphereOfInfluence> sois = new List<SphereOfInfluence>();

        public SimState(SimModel owner, Vector3 startPosition, Vector3 startVelocity, float startTime, float collisionRadius, float gravitationalConstant, float gravitationalRescaling)
        {
            this.owner = owner;
            this.collisionRadius = collisionRadius;
            this.gravitationalConstant = gravitationalConstant;
            this.gravitationalRescaling = gravitationalRescaling;
            this.position = startPosition;
            this.velocity = startVelocity;
            this.time = startTime;
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
                this.sois.Add(new SphereOfInfluence { g = bestG } );
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
                    this.sois.Add(new SphereOfInfluence { g = bestG });
                }
            }

            this.velocity += forceInfo.rescaledTotalForce * dt;

            var oldPosition = this.position;
            this.position += this.velocity * dt;


            this.crashed = false;
            for (int i = 0; i < this.owner.simGravitySources.Count; i++)
            {
                var g = this.owner.simGravitySources[i];
                var planetPosition = forceInfo.positions[i];

                var collision = Geometry.IntersectRaySphere(oldPosition, this.velocity.normalized, planetPosition, g.radius + this.collisionRadius);
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

        var allOrbits = GameObject.FindObjectsOfType<Orbit>();
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
        Debug.Assert(!allGravitySources.Any(g => g.gameObject.GetComponentInParent<Orbit>() != null && g.transform.localPosition != Vector3.zero));

        // Gravity sources with parent orbits (if the have one), and global positions (in case they don't).
        this.simGravitySources = allGravitySources
            .Select(g => new SimGravity {
                from = g,
                mass = g.parameters.mass, // we only need the mass, density is only required to calculate mass initially
                radius = g.radius, // radius is applied using local scale on the same game object as the gravity
                parent = this.orbits.IndexOf(g.gameObject.GetComponentInParent<Orbit>()),
                position = g.position
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

        for (int i = 0; i < this.simGravitySources.Count; i++)
        {
            var g = this.simGravitySources[i];
            var gPosition = g.parent != -1 ? orbitPositions[g.parent] : g.position;
            var force = OrbitalUtils.CalculateForce(
                gPosition - position,
                g.mass,
                gravitationalConstant);
            positions[i] = gPosition;
            forces[i] = force;
        }

        // Apply force rescaling
        var totalForce = Vector3.zero;
        var rescaledTotalForce = Vector3.zero;

        foreach (var force in forces)
        {
            totalForce += force;
            rescaledTotalForce += force; //.normalized * bestSoi.maxForce * Mathf.Pow(force.magnitude / bestSoi.maxForce, this.gravitationalRescaling);
        }

        return new ForceInfo {
            positions = positions,
            forces = forces,
            totalForce = totalForce,
            rescaledTotalForce = rescaledTotalForce
        };
    }

    public async Task<SimPath> CalculateSimPath(Vector3 position, Vector3 velocity, float startTime, float timeStep, int steps, float collisionRadius, float gravitationalConstant, float gravitationalRescaling)
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
            for (int i = 0; i < steps; i++)
            {
                state.Step(timeStep);
            }
        });

        return new SimPath { 
            dt = timeStep,
            finalVelocity = state.velocity,
            path = state.path.ToArray(),
            timeStart = startTime
        };
    }
}

public class SectionedSimPath
{
    public Vector3 position;
    public Vector3 velocity;

    readonly SimModel model;
    readonly float targetLength;
    readonly int sectionSteps;
    readonly float dt;
    readonly float gravitationalConstant;
    readonly float gravitationalRescaling;

    List<SimPath> pathSections = new List<SimPath>();
    float simTime;
    bool sectionIsQueued = false;
    SimPath lastValidPathSection = null;
    bool restartPath = true;

    public IEnumerable<Vector3> GetFullPath()
    {
        return pathSections.SelectMany(p => p.path);
    }

    public SectionedSimPath(SimModel model, float startSimTime, Vector3 startPosition, Vector3 startVelocity, float targetLength, float dt, float gravitationalConstant, float gravitationalRescaling, int sectionSteps = 100)
    {
        this.model = model;
        this.targetLength = targetLength;
        this.sectionSteps = sectionSteps;
        this.dt = dt;
        this.gravitationalConstant = gravitationalConstant;
        this.gravitationalRescaling = gravitationalRescaling;

        this.position = startPosition;
        this.velocity = startVelocity;
        this.simTime = startSimTime;
    }

    public void Step(float simTime, Vector3 force)
    {
        this.simTime = simTime;

        // Remove old sections
        while (this.pathSections.Count > 0 && this.pathSections[0].timeEnd <= this.simTime)
        {
            this.pathSections.RemoveAt(0);
        }

        // Get new position, either from the path or via dead reckoning
        if (this.pathSections.Count > 0 && force.magnitude == 0)
        {
            var newPosition = this.pathSections[0].GetPosition(this.simTime);
            this.velocity = (newPosition - this.position) / this.dt;
            this.position = newPosition;
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

        if (!this.sectionIsQueued && (this.restartPath || this.GetTotalPathDuration() < this.targetLength))
        {
            this.GenerateNewSection();
        }
    }

    float GetTotalPathDuration() => this.pathSections.Select(p => p.duration).Sum();

    async void GenerateNewSection()
    {
        this.sectionIsQueued = true;

        SimPath newSection;
        // TODO? Might need to avoid restarting the path until we calculated all the required sections of the old path
        //       In this case perhaps it makes no sense to calculate it in sections at all? Maybe for aesthetics and less laggy feeling,
        //       but we could still interpolate the whole path anyway...
        if (this.restartPath)
        {
            // Do this before the await, so we won't overwrite it if its set again
            this.restartPath = false;
            // TODO: collision radius
            newSection = await this.model.CalculateSimPath(this.position, this.velocity, this.simTime, this.dt, this.sectionSteps, 0, this.gravitationalConstant, this.gravitationalRescaling);
        }
        else
        {

            newSection = await this.model.CalculateSimPath(this.lastValidPathSection.finalPosition, this.lastValidPathSection.finalVelocity, this.lastValidPathSection.timeEnd, this.dt, this.sectionSteps, 0, this.gravitationalConstant, this.gravitationalRescaling);
        }
        // Insert path section in the correct order, removing any that are overlapping it
        this.pathSections = this.pathSections
            .Where(p => p.timeEnd <= newSection.timeStart)
            .Concat(new[] { newSection })
            .Concat(this.pathSections.Where(p => p.timeStart >= newSection.timeEnd)).ToList();
        this.lastValidPathSection = newSection;

        this.sectionIsQueued = false;
    }
}
