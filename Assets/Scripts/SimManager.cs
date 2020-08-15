using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    //class SimPlanet
    //{
    //    public SimPlanet parent;
    //    public List<SimPlanet> children = new List<SimPlanet>();

    //    public string name;
    //    public int sourceID;
    //    public OrbitParameters? orbit;
    //    public GravityParameters? gravity;
    //    //public Matrix4x4 localToWorldMatrix;
    //    public float radius = 0f;

    //    public Vector3 localPosition = Vector3.zero;
    //    public Vector3 position = Vector3.zero;

    //    public float mass => this.gravity?.mass ?? 0;

    //    public override string ToString() => this.name;

    //    public SimPlanet(GravitySource from)
    //    {
    //        this.gravity = from.parameters;
    //        this.radius = from.transform.localScale.x * 0.5f;
    //        this.localPosition = from.transform.localPosition;
    //        this.position = from.transform.position;
    //        var parentOrbit = from.GetComponentInParent<Orbit>();
    //        if (parentOrbit != null)
    //        {
    //            this.orbit = parentOrbit.parameters;
    //        }
    //    }

    //    public void UpdatePositions(float t)
    //    {
    //        if (this.orbit != null)
    //        {
    //            this.localPosition = this.orbit?.GetPosition(t) ?? Vector2.zero;
    //            this.position = this.parent != null ? this.parent.position + this.localPosition : this.localPosition;
    //        }
    //        //else
    //        //{
    //        //    if (this.parent != null)
    //        //    {
    //        //        this.localPosition = this.parent.localTransform.inverse.InverseTransformPoint(this.gravity.transform.position);
    //        //    }
    //        //    else
    //        //    {
    //        //        this.localPosition = this.gravity.transform.position;
    //        //    }
    //        //}

    //        foreach (var child in this.children)
    //        {
    //            child.UpdatePositions(t);
    //        }
    //    }

    //    public bool Collision(Vector3 otherPosition, float otherRadius)
    //    {
    //        return this.radius > 0 && Vector3.Distance(otherPosition, this.position) < otherRadius + this.radius
    //            || this.children.Any(c => c.Collision(otherPosition, otherRadius));
    //    }
    //}

    float radius;
    Vector3 position;
    Vector3 velocity;
    List<SimOrbit> orbits;
    List<SimGravity> gravitySources;
    float startTime;
    float simTime;

    List<Vector3> path;
    float pathLength;
    bool crashed;

    public static SimManager Instance = null;

    void Start()
    {
        Instance = this;
        this.startTime = Time.time;
        this.warningSign.SetActive(false);
    }

    void DelayedInit()
    {
        if (this.orbits != null)
            return;

        this.radius = GameLogic.Instance.player.GetComponentInChildren<MeshRenderer>().bounds.extents.x;

        var allOrbits = GameObject.FindObjectsOfType<Orbit>();
        var orderedOrbits = new List<Orbit>();
        var orbitStack = new Stack<Orbit>(allOrbits.Where(o => o.gameObject.GetComponentInParentOnly<Orbit>() == null));
        while(orbitStack.Any())
        {
            var orbit = orbitStack.Pop();
            orderedOrbits.Add(orbit);
            var directChildren = allOrbits.Where(o => o.gameObject.GetComponentInParentOnly<Orbit>() == orbit);
            orderedOrbits.AddRange(directChildren.Reverse());
        }

        // NOTE: We assume that orbits have no relative offsets other than those driven by the orbit parameters
        // i.e. localPosition in the hierarchy is 0 all the way to the root.
        //bool ValidateOrbits()
        //{
        //    var invalidOrbits = orderedOrbits.Where(o => o.GetAllParents().Any(p => p.GetComponent<Orbit>() == null && p.transform.localPosition != Vector3.zero));
        //    if(invalidOrbits.Any())
        //    {
                
        //    }
        //}
        //Debug.Assert(!orderedOrbits.Any(o => o.GetAllParents().Any(p => p.GetComponent<Orbit>() == null && p.transform.localPosition != Vector3.zero)));

        // Orbits ordered in depth first search ordering, with parent indices
        this.orbits = orderedOrbits.Select(
            o => new SimOrbit {
#if DEBUG
                name = o.ToString(),
#endif
                orbit = o.parameters,
                parent = orderedOrbits.IndexOf(o.gameObject.GetComponentInParentOnly<Orbit>())
            }).ToList();

        // NOTE: We assume that if the gravity source has a parent orbit then its local position is 0, 0, 0.
        var allGravitySources = GameObject.FindObjectsOfType<GravitySource>();
        Debug.Assert(!allGravitySources.Any(g => g.gameObject.GetComponentInParent<Orbit>() != null && g.transform.localPosition != Vector3.zero));

        // Gravity sources with parent orbits (if the have one), and global positions (in case they don't).
        this.gravitySources = allGravitySources
            .Select(g => new SimGravity {
#if DEBUG
                name = g.ToString(),
#endif
                mass = g.parameters.mass, // we only need the mass, density is only required to calculate mass initially
                radius = g.transform.localScale.x * 0.5f, // radius is applied using local scale on the same game object as the gravity
                parent = orderedOrbits.IndexOf(g.gameObject.GetComponentInParent<Orbit>()),
                position = g.transform.position
            }).ToList();



        //// Add all gravity sources
        //var planets = GameObject.FindObjectsOfType<GravitySource>()
        //    .Select(g => new { gravitySource = g, orbit = g.GetComponentInParent<Orbit>() }).ToList();

        //// Add all orbits (unless their GameObject was already added as a gravity source above)
        //planets.AddRange(
        //    GameObject.FindObjectsOfType<Orbit>()
        //        .Where(o => !planets.Any(p => p.orbit == o))
        //        .Select(o => new { gravitySource = default(GravitySource), orbit = o })
        //);

        //// Set parent objects
        //foreach (var planet in planets.Where(p => p.orbit != null))
        //{
        //    var parentOrbit = planet.orbit.gameObject.GetComponentInParentOnly<Orbit>();
        //    if (parentOrbit != null)
        //    {
        //        planet.parent = planets.FirstOrDefault(p => p.orbit == parentOrbit);
        //        planet.parent.children.Add(planet);
        //    }
        //}

        //// We only need to remember the root planets for simulation, as it is must be done recursively from the root
        //this.rootPlanets = planets.Where(p => p.parent == null).ToList();
        //// Keep a handy list of gravity sources, as we evaluate gravity force using just a flat list of sources, not recursion
        //this.gravitySources = planets.Where(p => p.gravitySource != null).ToList();
    }

    void FixedUpdate()
    {
        this.DelayedInit();

        if(this.path == null)
        {
            this.position = GameLogic.Instance.player.transform.position;
            this.velocity = GameLogic.Instance.player.GetComponent<PlayerLogic>().velocity;
            this.simTime = Time.time;
            this.path = new List<Vector3>(2000);
            this.pathLength = 0;
            this.crashed = false;
        }

        for (int i = 0;
            i < GameConstants.Instance.SimStepsPerFrame && this.pathLength < GameConstants.Instance.SimDistanceLimit && !this.crashed; 
            i++)
        {
            var prevPos = this.position;
            this.Step(Time.fixedDeltaTime);
            this.path.Add(this.position);
            this.pathLength += Vector3.Distance(prevPos, this.position);
        }

        if (this.pathLength >= GameConstants.Instance.SimDistanceLimit || this.crashed)
        {
            this.pathRenderer.positionCount = this.path.Count;
            this.pathRenderer.SetPositions(this.path.ToArray());

            if(this.crashed && this.path.Count > 0)
            {
                this.warningSign.SetActive(true);
                var rectTransform = this.warningSign.GetComponent<RectTransform>();
                var canvas = this.warningSign.GetComponent<Graphic>().canvas;
                var canvasSafeArea = canvas.ScreenToCanvasRect(Screen.safeArea);
                var targetCanvasPosition = canvas.WorldToCanvasPosition(this.path.Last());
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

            this.path = null;
        }
    }

    public void Step(float dt)
    {
        this.simTime += dt;

        // TODO: use System.Buffers ArrayPool<Vector3>.Shared; (needs a package installed)
        var orbitPositions = new Vector3[this.orbits.Count];

        for (int i = 0; i < this.orbits.Count; i++)
        {
            var o = this.orbits[i];
            var localPosition = (Vector3)o.orbit.GetPosition(this.simTime - this.startTime);
            orbitPositions[i] = o.parent != -1 ? orbitPositions[o.parent] + localPosition : localPosition;
        }

        //bool Collision(Vector3 position, float radius)
        //{
        //    return radius > 0 && Vector3.Distance(this.position, position) < this.radius + radius;
        //}

        var force = Vector3.zero;

        foreach (var g in this.gravitySources)
        {
            var position = g.parent != -1 ? orbitPositions[g.parent] : g.position;
            force += GravityParameters.CalculateForce(this.position, position, g.mass);
        }

        this.velocity += force * dt;

        var oldPosition = this.position;
        this.position += this.velocity * dt;

        this.crashed = false;
        foreach (var g in this.gravitySources)
        {
            var planetPosition = g.parent != -1 ? orbitPositions[g.parent] : g.position;
            // TODO: interpolate crash position onto surface (sphere-line intersect from our last position to our current one)
            var collision = Geometry.IntersectRaySphere(oldPosition, this.position - oldPosition, planetPosition, g.radius + this.radius);
            if (collision.occurred)//Collision(planetPosition, g.radius))
            {
                //var collision = Geometry.IntersectRaySphere(oldPosition, this.position - oldPosition, planetPosition, g.radius + this.radius);
                this.position = collision.at;
                this.crashed = true;
                break;
            }
        }
    }
}
