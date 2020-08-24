using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

public class SimManager : MonoBehaviour
{
    public GameConstants constants;

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
        public OrbitParameters2.OrbitPath orbit;
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
    List<Orbit2> orbits;

    // Orbit2 parameters of all simulated bodies
    List<SimOrbit> simOrbits;

    // Gravity parameters of all simulated bodies
    List<SimGravity> simGravitySources;

    // Task representing the current instance of the sim path update task
    Task updatingPathTask = null;

    //float pathLength = 0;

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
                var localPosition = o.orbit.GetPosition(this.time);
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
                force += GravityParameters.CalculateForce(this.position, position, g.mass, this.owner.constants.GravitationalConstant);
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

    void OnValidate()
    {
        Assert.IsNotNull(this.constants);
        if (this.pathRenderer != null)
        {
            this.pathRenderer.positionCount = 0;
            this.pathRenderer.SetPositions(new Vector3[] { });
        }
    }

    void Start()
    {
        Assert.IsNotNull(this.constants);
        this.warningSign.SetActive(false);
    }

    void FixedUpdate()
    {
        this.DelayedInit();

        // Update "real" orbits and player
        foreach (var o in this.orbits)
        {
            o.SimUpdate();
        }

        GameLogic.Instance.player.GetComponent<PlayerLogic>().SimUpdate();


        this.UpdatePathAsync();

        this.UpdatePathWidth();
    }

    void DelayedInit()
    {
        if (this.orbits != null)
            return;

        this.radius = GameLogic.Instance.player.GetComponentInChildren<MeshRenderer>().bounds.extents.x;

        var allOrbits = GameObject.FindObjectsOfType<Orbit2>();
        this.orbits = new List<Orbit2>();
        var orbitStack = new Stack<Orbit2>(allOrbits.Where(o => o.gameObject.GetComponentInParentOnly<Orbit2>() == null));
        while(orbitStack.Any())
        {
            var orbit = orbitStack.Pop();
            this.orbits.Add(orbit);
            var directChildren = allOrbits.Where(o => o.gameObject.GetComponentInParentOnly<Orbit2>() == orbit);
            this.orbits.AddRange(directChildren.Reverse());
        }

        // Orbits ordered in depth first search ordering, with parent indices
        this.simOrbits = this.orbits.Select(
            o => new SimOrbit {
#if DEBUG
                name = o.ToString(),
#endif
                orbit = o.orbitPath,
                parent = this.orbits.IndexOf(o.gameObject.GetComponentInParentOnly<Orbit2>())
            }).ToList();

        // NOTE: We assume that if the gravity source has a parent orbit then its local position is 0, 0, 0.
        var allGravitySources = GravitySource.All();
        Debug.Assert(!allGravitySources.Any(g => g.gameObject.GetComponentInParent<Orbit2>() != null && g.transform.localPosition != Vector3.zero));

        // Gravity sources with parent orbits (if the have one), and global positions (in case they don't).
        this.simGravitySources = allGravitySources
            .Select(g => new SimGravity {
#if DEBUG
                name = g.ToString(),
#endif
                mass = g.parameters.mass, // we only need the mass, density is only required to calculate mass initially
                radius = g.radius, // radius is applied using local scale on the same game object as the gravity
                parent = this.orbits.IndexOf(g.gameObject.GetComponentInParent<Orbit2>()),
                position = g.position
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

        // timeStep *must* match what the normal calculate uses
        float timeStep = Time.fixedDeltaTime;
        float pathLengthLimit = this.constants.SimDistanceLimit;

        // Hand off to another thread
        await Task.Run(() =>
        {
            while(state.pathLength < pathLengthLimit && !state.crashed) {
                state.Step(timeStep);
            }
        });

        // Check Application.isPlaying to avoid the case where game was stopped before the task was finished (in editor).
        if (Application.isPlaying && this.pathRenderer != null && this.warningSign != null)
        {
            // Resume in main thread
            this.pathRenderer.positionCount = state.path.Count;
            this.pathRenderer.SetPositions(state.path.ToArray());
            //this.pathLength = state.pathLength;

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

    void UpdatePathWidth()
    {
        //this.pathRenderer.startWidth = 0;
        //this.pathRenderer.endWidth = (1 + 9 * this.pathLength / GameConstants.Instance.SimDistanceLimit);
        // Fixed width line in screen space:
        this.pathRenderer.startWidth = this.pathRenderer.endWidth = this.constants.SimLineWidth;
    }
}
