using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    public SolarSystem a;
    public SolarSystem b;
}

public class SolarSystem
{
    public Body main;

    public List<Body> comets = new List<Body>();
    public List<Belt> belts = new List<Belt>();

    public List<Link> links = new List<Link>();

    public void Load(GameObject root)
    {
        var systemObjectTransform = root.transform.Find("System");
        if(systemObjectTransform != null)
        {
            Object.Destroy(systemObjectTransform.gameObject);
        }
        
        var systemObject = new GameObject("System");
        systemObject.transform.SetParent(root.transform, worldPositionStays: false);

        var rootBody = this.main.InstanceHierarchy();
        rootBody.transform.SetParent(systemObject.transform, worldPositionStays: false);
    }
}

public class Map
{
    public List<SolarSystem> systems = new List<SolarSystem>();
}

public class MapComponent : MonoBehaviour
{
    public BodySpecs bodySpecs;

    public Map map;

    static SolarSystem GenerateSystem(BodySpecs bodySpecs)
    {
        var sys = new SolarSystem();

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

        for (int i = 0; i < systems; i++)
        {
            map.systems.Add(GenerateSystem(bodySpecs));
        }

        return map;
    }

    void LoadRandomSystem()
    {
        this.map.systems[Random.Range(0, this.map.systems.Count)].Load(this.gameObject);
    }

    // Start is called before the first frame update
    void Awake()
    {
        this.map = Generate(this.bodySpecs, 1);

        this.LoadRandomSystem();
    }

}
