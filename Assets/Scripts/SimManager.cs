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

    class SimPlanet
    {
        public SimPlanet parent;
        public List<SimPlanet> children = new List<SimPlanet>();

        public Orbit orbit;
        public GravitySource gravitySource;
        public float radius = 0f;

        public Vector3 localPosition = Vector3.zero;
        public Vector3 position = Vector3.zero;

        public float mass => this.gravitySource.parameters.mass;

        public override string ToString() => (this.orbit != null ? this.orbit.gameObject : this.gravitySource.gameObject).ToString();

        //public SimPlanet()
        //{
        //    this.parent = parent;
        //    this.orbit = orbit;
        //    this.gravitySource = gravitySource;

        //    this.localPosition = from.transform.position - parent.position;
        //    this.UpdatePosition();
        //}

        public void UpdatePositions(float t)
        {
            if (orbit != null)
            {
                this.localPosition = this.orbit.parameters.GetPosition(t);
            }
            else
            {
                if (this.parent != null)
                {
                    this.localPosition = this.parent.orbit.transform.InverseTransformPoint(this.gravitySource.transform.position);
                }
                else
                {
                    this.localPosition = this.gravitySource.transform.position;
                }
            }
            if (this.parent != null)
            {
                this.position = this.parent.position + this.localPosition;
            }
            else
            {
                this.position = this.localPosition;
            }
            foreach (var child in this.children)
            {
                child.UpdatePositions(t);
            }
        }

        public bool Collision(Vector3 otherPosition, float otherRadius)
        {
            if (this.radius > 0 && Vector3.Distance(otherPosition, this.position) < otherRadius + this.radius)
                return true;
            return this.children.Any(c => c.Collision(otherPosition, otherRadius));
        }
    }

    float radius;
    Vector3 position;
    Vector3 velocity;
    List<SimPlanet> rootPlanets;
    List<SimPlanet> gravitySources;
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
        if (this.rootPlanets != null)
            return;

        this.radius = GameLogic.Instance.player.GetComponentInChildren<MeshRenderer>().bounds.extents.x;

        // Add all gravity sources
        var planets = GameObject.FindObjectsOfType<GravitySource>()
            .Select(g => new SimPlanet {
                gravitySource = g,
                radius = g.transform.localScale.x * 0.5f,
                orbit = g.GetComponentInParent<Orbit>()
            }).ToList();

        // Add all orbits (unless their GameObject was already added as a gravity source above)
        planets.AddRange(
            GameObject.FindObjectsOfType<Orbit>()
                .Where(o => !planets.Any(p => p.orbit == o))
                .Select(o => new SimPlanet { orbit = o })
        );

        // Set parent objects
        foreach (var planet in planets.Where(p => p.orbit != null))
        {
            var parentOrbit = planet.orbit.gameObject.GetComponentInParentOnly<Orbit>();
            if (parentOrbit != null)
            {
                planet.parent = planets.FirstOrDefault(p => p.orbit == parentOrbit);
                planet.parent.children.Add(planet);
            }
        }

        // We only need to remember the root planets for simulation, as it is must be done recursively from the root
        this.rootPlanets = planets.Where(p => p.parent == null).ToList();
        // Keep a handy list of gravity sources, as we evaluate gravity force using just a flat list of sources, not recursion
        this.gravitySources = planets.Where(p => p.gravitySource != null).ToList();
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
        foreach (var rootPlanet in this.rootPlanets)
        {
            rootPlanet.UpdatePositions(this.simTime - this.startTime);
        }
        var force = this.gravitySources
                .Select(src => GravityParameters.CalculateForce(this.position, src.position, src.mass))
                .Aggregate((a, b) => a + b);
        this.velocity += force * dt;
        this.position += this.velocity * dt;

        this.crashed = this.rootPlanets.Any(p => p.Collision(this.position, this.radius));
    }
}
