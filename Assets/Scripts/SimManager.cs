using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class SimManager : MonoBehaviour {
    [Tooltip("Used to render the simulated path")]
    public LineRenderer pathRenderer;
    [Tooltip("Used to indicate a predicted crash")]
    public GameObject warningSign;

    struct SimOrbit
    {
#if DEBUG
        public string name;
#endif
        public int parent;
        public OrbitParameters orbit;
    }

    struct SimGravity
    {
#if DEBUG
        public string name;
#endif
        public int parent;
        public float mass;
        public float radius;
        public Vector3 position;
    }

    // Radius of the player
    float radius;

    // Real Orbits for updating
    List<Orbit> orbits;

    // Orbit parameters of all simulated bodies
    List<SimOrbit> simOrbits;

    // Gravity parameters of all simulated bodies
    List<SimGravity> simGravitySources;

    // Task representing the current instance of the sim path update task
    Task updatingPathTask = null;

    #region SimState
    // Represents the current state of a simulation
    class SimState
    {
        readonly SimManager owner;
        Vector3 position;
        Vector3 velocity;
        float time;
        public readonly List<Vector3> path = new List<Vector3>();
        public float pathLength = 0;
        public bool crashed;

        public SimState(SimManager owner, Vector3 startPosition, Vector3 startVelocity, float startTime)
        {
            this.owner = owner;
            this.position = startPosition;
            this.velocity = startVelocity;
            this.time = startTime;
        }

        public void Step(float dt)
        {
            this.time += dt;

            // TODO: use System.Buffers ArrayPool<Vector3>.Shared; (needs a package installed)
            var orbitPositions = new Vector3[this.owner.simOrbits.Count];

            for (int i = 0; i < this.owner.simOrbits.Count; i++)
            {
                var o = this.owner.simOrbits[i];
                var localPosition = (Vector3)o.orbit.GetPosition(this.time);
                orbitPositions[i] = o.parent != -1 ? orbitPositions[o.parent] + localPosition : localPosition;
            }

            //bool Collision(Vector3 position, float radius)
            //{
            //    return radius > 0 && Vector3.Distance(this.position, position) < this.radius + radius;
            //}

            var force = Vector3.zero;

            foreach (var g in this.owner.simGravitySources)
            {
                var position = g.parent != -1 ? orbitPositions[g.parent] : g.position;
                force += GravityParameters.CalculateForce(this.position, position, g.mass);
            }

            this.velocity += force * dt;

            var oldPosition = this.position;
            this.position += this.velocity * dt;


            this.crashed = false;
            foreach (var g in this.owner.simGravitySources)
            {
                var planetPosition = g.parent != -1 ? orbitPositions[g.parent] : g.position;
                
                // This collision detection method was intended to address jittering of the predicted collision position,
                // presumed to be due to inaccurate evaluation of the collision position,
                // however the rendering of the position still jitters, so there must be another cause for this (perhaps mismatch of 
                // start time with rendering?).
                var collision = Geometry.IntersectRaySphere(oldPosition, this.velocity.normalized, planetPosition, g.radius + this.owner.radius);
                if (collision.occurred && collision.t < this.velocity.magnitude * dt * 3)//Collision(planetPosition, g.radius))
                {
                    //var collision = Geometry.IntersectRaySphere(oldPosition, this.position - oldPosition, planetPosition, g.radius + this.radius);
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

    public static SimManager Instance = null;

    void Start()
    {
        Instance = this;
        this.warningSign.SetActive(false);
    }

    void DelayedInit()
    {
        if (this.orbits != null)
            return;

        this.radius = GameLogic.Instance.player.GetComponentInChildren<MeshRenderer>().bounds.extents.x;

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
#if DEBUG
                name = o.ToString(),
#endif
                orbit = o.parameters,
                parent = this.orbits.IndexOf(o.gameObject.GetComponentInParentOnly<Orbit>())
            }).ToList();

        // NOTE: We assume that if the gravity source has a parent orbit then its local position is 0, 0, 0.
        var allGravitySources = GravitySource.All();
        Debug.Assert(!allGravitySources.Any(g => g.gameObject.GetComponentInParent<Orbit>() != null && g.transform.localPosition != Vector3.zero));

        // Gravity sources with parent orbits (if the have one), and global positions (in case they don't).
        this.simGravitySources = allGravitySources
            .Select(g => new SimGravity {
#if DEBUG
                name = g.ToString(),
#endif
                mass = g.parameters.mass, // we only need the mass, density is only required to calculate mass initially
                radius = g.transform.localScale.x * 0.5f, // radius is applied using local scale on the same game object as the gravity
                parent = this.orbits.IndexOf(g.gameObject.GetComponentInParent<Orbit>()),
                position = g.transform.position
            }).ToList();
    }

    async Task UpdatePath()
    {
        var playerLogic = GameLogic.Instance.player.GetComponent<PlayerLogic>();
        var state = new SimState(
            owner: this,
            startPosition: playerLogic.simPosition,
            startVelocity: playerLogic.velocity,
            startTime: GameLogic.Instance.simTime
        );

        float timeStep = Time.fixedDeltaTime; //GameConstants.Instance.SimStepDt;//Time.fixedDeltaTime;

        // Hand off to another thread
        await Task.Run(() =>
        {
            while(state.pathLength < GameConstants.Instance.SimDistanceLimit && !state.crashed) {
                state.Step(timeStep);
            }
        });

        if (this.pathRenderer != null && this.warningSign != null)
        {
            // Resume in main thread
            this.pathRenderer.positionCount = state.path.Count;
            this.pathRenderer.SetPositions(state.path.ToArray());

            if (state.crashed && state.path.Count > 0)
            {
                this.warningSign.SetActive(true);
                var rectTransform = this.warningSign.GetComponent<RectTransform>();
                var canvas = this.warningSign.GetComponent<Graphic>().canvas;
                var canvasSafeArea = canvas.ScreenToCanvasRect(Screen.safeArea);
                var targetCanvasPosition = canvas.WorldToCanvasPosition(state.path.Last());
                var clampArea = new Rect(
                    canvasSafeArea.x - rectTransform.rect.x,
                    canvasSafeArea.y - rectTransform.rect.y,
                    canvasSafeArea.width - rectTransform.rect.width,
                    canvasSafeArea.height - rectTransform.rect.height
                );
                rectTransform.anchoredPosition = clampArea.ClampToRectOnRay(targetCanvasPosition);
            }
            else
            {
                this.warningSign.SetActive(false);
            }
        }
    }

    async void UpdatePathAsync()
    {
        if (this.updatingPathTask == null)
        {
            this.updatingPathTask = this.UpdatePath();
            // Hand off to the other thread
            await this.updatingPathTask;
            // Back on the main thread
            this.updatingPathTask = null;
        }
    }

    void UpdateOrbitWidth()
    {
        this.pathRenderer.startWidth = this.pathRenderer.endWidth = GameConstants.Instance.SimLineWidth;
    }

    void FixedUpdate()
    {
        this.DelayedInit();

        // Update "real" orbits and player
        foreach(var o in this.orbits)
        {
            o.SimUpdate();
        }

        GameLogic.Instance.player.GetComponent<PlayerLogic>().SimUpdate();


        this.UpdatePathAsync();

        this.UpdateOrbitWidth();
    }

}
