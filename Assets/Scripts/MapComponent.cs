using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

public abstract class Body
{
    public GameObject prefab;
    public OrbitParameters parameters = OrbitParameters.Zero;

    public List<Body> children = new List<Body>();

    public GameObject InstanceHierarchy()
    {
        var self = this.Instance();
        foreach (var child in this.children)
        {
            var childInstance = child.Instance();
            childInstance.transform.SetParent(self.GetComponent<Orbit>().position.transform, worldPositionStays: false);
        }
        return self;
    }

    protected virtual GameObject Instance()
    {
        var obj = Object.Instantiate(this.prefab);
        obj.GetComponent<Orbit>().parameters = this.parameters;
        return obj;
    }
}

public class Sun : Body
{
    public float brightness;

    protected override GameObject Instance()
    {
        return base.Instance();
    }
}

public class Planet : Body
{
    protected override GameObject Instance()
    {
        return base.Instance();
    }
}

public class Belt
{
    public GameObject prefab;
    public float radius;
    public float width;
}

public class Link
{
    public SolarSystem from;
    public SolarSystem to;

    public bool Match(SolarSystem from, SolarSystem to)
    {
        return this.from == from && this.to == to ||
            this.from == to && this.to == from;
    }
}

public class SolarSystem
{
    public Vector2 position;

    public Body main;

    public string name;

    public List<Body> comets = new List<Body>();
    public List<Belt> belts = new List<Belt>();

    public static void Unload(GameObject root)
    {
        var systemObjectTransform = root.transform.Find("System");
        if (systemObjectTransform != null)
        {
            Object.Destroy(systemObjectTransform.gameObject);
        }
    }

    public void Load(GameObject root)
    {
        Unload(root);
        var systemObject = new GameObject("System");
        systemObject.transform.SetParent(root.transform, worldPositionStays: false);

        var rootBody = this.main.InstanceHierarchy();
        rootBody.transform.SetParent(systemObject.transform, worldPositionStays: false);
    }
}

public class Map
{
    public List<Link> links = new List<Link>();
    public List<SolarSystem> systems = new List<SolarSystem>();
}

public class MapComponent : MonoBehaviour
{
    public BodySpecs bodySpecs;

    public Map map;

    public SolarSystem currentSystem;

    Lazy<List<SolarSystem>> jumpTargets = new Lazy<List<SolarSystem>>(() => new List<SolarSystem>());

    PlayerController player => FindObjectOfType<PlayerController>();

    // Start is called before the first frame update
    void Awake()
    {
        this.map = Generate(this.bodySpecs, 50);
    }

    void Start()
    {
        this.LoadRandomSystem();
    }

    public SolarSystem GetJumpTarget()
    {
        var playerDirection = this.player.transform.position;

        return this.jumpTargets.Value
            .OrderBy(t => Vector2.Angle(t.position - this.currentSystem.position, playerDirection)).FirstOrDefault();
    }

    public bool CanJump()
    {
        return this.GetJumpTarget() != null;
    }

    public async Task JumpAsyc()
    {
        Assert.IsNotNull(this.GetJumpTarget());

        await this.Jump(this.GetJumpTarget());
    }

    public async Task Jump(SolarSystem target)
    {
        if(this.currentSystem == target)
        {
            Debug.Log("Jumped to current system");
            return;
        }

        // Player enter warp

        // Disable player input
        this.player.GetComponent<PlayerController>().enabled = false;

        var travelVec = this.currentSystem != null
            ? (target.position - this.currentSystem.position).normalized
            : (Vector2)(Quaternion.Euler(0, 0, Random.Range(0f, 360f)) * Vector3.one)
            ;

        // Send player into warp
        var playerSimMovement = this.player.GetComponent<SimMovement>();
        var playerWarpController = this.player.GetComponent<WarpController>();
        playerSimMovement.enabled = false;
        playerWarpController.enabled = true;

        foreach (var collider in this.player.GetComponentsInChildren<Collider>())
        {
            collider.enabled = false;
        }

        await playerWarpController.EnterWarpAsync(playerSimMovement.velocity, 50);

        // Set this before refreshing the sim so it is applied correctly

        Vector2 landingPosition;
        Vector2 landingVelocity;
        if (this.currentSystem != null)
        {
            var inTravelVec = (target.position - this.currentSystem.position).normalized;
            // var positionVec = Vector2.Perpendicular(inTravelVec) * (Random.value > 0.5f ? -1 : 1);

            landingPosition = inTravelVec * -Random.Range(30f, 50f) + Vector2.Perpendicular(inTravelVec) * Random.Range(-20f, +20f);
            landingVelocity = inTravelVec * Random.Range(0.5f, 1f);
        }
        else
        {
            landingPosition = Quaternion.Euler(0, 0, Random.Range(0f, 360f)) * Vector2.one * Random.Range(30f, 50f);
            landingVelocity = Quaternion.Euler(0, 0, Random.Range(0f, 360f)) * Vector2.one * Random.Range(0.5f, 1f);
        }

        await playerWarpController.TurnInWarpAsync(landingVelocity.normalized);

        // Destroy old system, update player position and create new one
        this.currentSystem = target;
        this.currentSystem.Load(this.gameObject);

        this.jumpTargets = new Lazy<List<SolarSystem>>(() => {
            return this.map.links
                .Where(l => l.from == this.currentSystem || l.to == this.currentSystem)
                .Select(l => l.from == this.currentSystem ? l.to : l.from)
                .ToList();
        });

        await playerWarpController.ExitWarpAsync(landingPosition, landingVelocity.normalized, landingVelocity.magnitude);

        // Safe to turn collision back on hopefully
        foreach (var collider in this.player.GetComponentsInChildren<Collider>())
        {
            collider.enabled = true;
        }

        playerWarpController.enabled = false;

        playerSimMovement.startVelocity = landingVelocity;
        playerSimMovement.enabled = true;

        FindObjectOfType<SimManager>().Refresh();


        // Re-eable player input
        this.player.GetComponent<PlayerController>().enabled = true;
    }

    public void LoadRandomSystem()
    {
        _ = this.Jump(this.map.systems[Random.Range(0, this.map.systems.Count)]);
    }

    static SolarSystem GenerateSystem(BodySpecs bodySpecs, string name, Vector2 position)
    {
        var sys = new SolarSystem { name = name, position = position };

        var mainSpec = bodySpecs.RandomSun();
        sys.main = new Sun { prefab = mainSpec.prefab };

        // Place holder system generation.
        // TODO: make this way better lol
        // Probably we want a set of different generators for certain types of systems?
        int planets = Random.Range(0, 7);
        float minRadius = mainSpec.radius + mainSpec.radiusRange + 10;
        float radius = Random.Range(minRadius, minRadius * 2);
        for (int i = 0; i < planets; i++)
        {
            var planetSpec = bodySpecs.RandomPlanet();
            var planet = new Planet
            {
                prefab = planetSpec.prefab,
                parameters = new OrbitParameters
                {
                    periapsis = radius,
                    apoapsis = radius
                }
            };

            sys.main.children.Add(planet);

            radius *= Random.Range(1.25f, 2.25f);
        }

        return sys;
    }

    static Map Generate(BodySpecs bodySpecs, int systems)
    {
        var map = new Map();

        float minDistance = 0.01f;

        // Generate the systems
        for (int i = 0; i < systems; i++)
        {
            // TODO: something more controlled
            var position = new Vector2(Random.value, Random.value);
            for (int j = 0; j < 50 && map.systems.Any(s => Vector2.Distance(s.position, position) < minDistance); j++)
            {
                position = new Vector2(Random.value, Random.value);
            }

            char RandomLetter() => (char)((int)'A' + Random.Range(0, 'Z' - 'A'));
            string name = $"{RandomLetter()}{RandomLetter()}-{Mathf.FloorToInt(position.x * 100)},{Mathf.FloorToInt(position.y * 100)}";

            map.systems.Add(GenerateSystem(bodySpecs, name, position));
        }

        // Add the links
        // TODO: more interesting links, create choke points, avoid cross over
        foreach (var s in map.systems)
        {
            var existingTargets = map.links.Where(l => l.to == s).Select(s2 => s2.from);
            var newLinks = map.systems
                .Except(existingTargets)
                .OrderBy(s2 => Vector2.Distance(s.position, s2.position))
                .Skip(1) // skip self
                .Take(Random.Range(1, 4) - existingTargets.Count());
            map.links.AddRange(newLinks.Select(n => new Link { from = s, to = n }).ToList());
        }

        return map;
    }
}
