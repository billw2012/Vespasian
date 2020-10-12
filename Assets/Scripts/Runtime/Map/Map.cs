using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

public abstract class Body
{
    public string specId;
    public int randomKey;

    public abstract GameObject Instance(BodySpecs bodySpecs, float systemDanger);

    public abstract void Apply(GameObject target);
}

public class StarOrPlanet : Body
{
    public OrbitParameters parameters = OrbitParameters.Zero;

    public List<StarOrPlanet> children = new List<StarOrPlanet>();

    public float temp;
    public float density;
    public float radius;
    public float mass;

    public override GameObject Instance(BodySpecs bodySpecs, float systemDanger)
    {
        var self = this.InstanceSelf(bodySpecs, systemDanger);
        foreach (var child in this.children)
        {
            var childInstance = child.Instance(bodySpecs, systemDanger);
            childInstance.transform.SetParent(self.GetComponent<Orbit>().position.transform, worldPositionStays: false);
        }
        return self;
    }

    public override void Apply(GameObject target)
    {
        // Orbit setup
        var orbit = target.GetComponent<Orbit>();
        if (orbit != null)
        {
            orbit.parameters = this.parameters;
        }

        // Body characteristics
        var bodyLogic = target.GetComponent<BodyLogic>();
        if (bodyLogic != null)
        {
            bodyLogic.radius = this.radius;
            bodyLogic.dayPeriod = MathX.RandomGaussian(5, 30 * this.mass) * Mathf.Sign(Random.value - 0.5f);
        }

        // Gravity
        var gravitySource = target.GetComponent<GravitySource>();
        if (gravitySource)
        {
            gravitySource.autoMass = false;
            gravitySource.parameters.mass = this.mass;
            gravitySource.parameters.density = this.density;
            //float volume = 4f * Mathf.PI * Mathf.Pow(body.density, 3) / 3f;
            //gravitySource.parameters.density = body.mass / volume;
        }
    }

    GameObject InstanceSelf (BodySpecs bodySpecs, float systemDanger)
    {
        var spec = bodySpecs.GetSpecById(this.specId);
        var obj = Object.Instantiate(spec.prefab);
        obj.GetComponent<BodyGenerator>().Init(this, systemDanger);
        return obj;
    }
}

public class Belt : Body
{
    public float radius;
    public float width;
    public OrbitParameters.OrbitDirection direction;

    public override GameObject Instance(BodySpecs bodySpecs, float systemDanger)
    {
        var spec = bodySpecs.GetSpecById(this.specId);

        var obj = Object.Instantiate(spec.prefab);

        obj.GetComponent<BodyGenerator>().Init(this, systemDanger);

        return obj;
    }

    public override void Apply(GameObject target)
    {
        var asteroidRing = target.GetComponent<AsteroidRing>();
        asteroidRing.radius = this.radius;
        asteroidRing.width = this.width;
        asteroidRing.direction = this.direction;
    }
}

public class Comet : Body
{
    public string prefabId;

    public OrbitParameters parameters = OrbitParameters.Zero;

    public override GameObject Instance(BodySpecs bodySpecs, float systemDanger)
    {
        var spec = bodySpecs.GetSpecById(this.specId);
        var obj = Object.Instantiate(spec.prefab);
        obj.GetComponent<BodyGenerator>().Init(this, systemDanger);
        return obj;
    }

    public override void Apply(GameObject target)
    {
        // Orbit setup
        var orbit = target.GetComponent<Orbit>();
        if (orbit != null)
        {
            orbit.parameters = this.parameters;
        }
    }
}

public class Link : IEquatable<Link>
{
    public SolarSystem from;
    public SolarSystem to;

    public bool Match(SolarSystem from, SolarSystem to)
    {
        return this.from == from && this.to == to ||
            this.from == to && this.to == from;
    }

    public override bool Equals(object obj)
    {
        return this.Match(((Link)obj).from, ((Link)obj).to);
    }

    public bool Equals(Link other)
    {
        return other != null && this.Match(other.from, other.to);
    }

    public override int GetHashCode()
    {
        int hashCode = -1951484959;
        hashCode = hashCode * -1521134295 + (EqualityComparer<SolarSystem>.Default.GetHashCode(this.from) + EqualityComparer<SolarSystem>.Default.GetHashCode(this.to));
        return hashCode;
    }

    public static bool operator ==(Link left, Link right) => EqualityComparer<Link>.Default.Equals(left, right);
    public static bool operator !=(Link left, Link right) => !(left == right);
}

public class SolarSystem
{
    public Vector2 position;

    public StarOrPlanet main;

    public string name;

    public float danger;

    public List<Comet> comets = new List<Comet>();
    public List<Belt> belts = new List<Belt>();

    public static void Unload(GameObject root)
    {
        var systemObjectTransform = root.transform.Find("System");
        if (systemObjectTransform != null)
        {
            Object.Destroy(systemObjectTransform.gameObject);
        }
    }

    public async Task LoadAsync(BodySpecs bodySpecs, GameObject root)
    {
        var rootBody = this.main.Instance(bodySpecs, this.danger);
        foreach (var belt in this.belts)
        {
            var beltObject = belt.Instance(bodySpecs, this.danger);
            beltObject.transform.SetParent(rootBody.transform);
        }
        foreach (var comet in this.comets)
        {
            var cometObject = comet.Instance(bodySpecs, this.danger);
            cometObject.transform.SetParent(rootBody.transform);
        }
        // We load the new system first and wait for it before unloading the previous one
        await new WaitUntil(() => rootBody.activeSelf);

        Unload(root);

        var systemObject = new GameObject("System");
        systemObject.transform.SetParent(root.transform, worldPositionStays: false);

        rootBody.transform.SetParent(systemObject.transform, worldPositionStays: false);
    }
}

public class Map
{
    public List<Link> links = new List<Link>();
    public List<SolarSystem> systems = new List<SolarSystem>();

    public IEnumerable<(SolarSystem system, Link link)> GetJumpTargets(SolarSystem system)
    {
        return this.links
            .Where(l => l.from == system || l.to == system)
            .Select(l => (system: l.from == system ? l.to : l.from, link: l));
    }

    public void RemoveLink(Link link)
    {
        this.links.Remove(link);
    }

    public void RemoveSystem(SolarSystem system)
    {
        this.RemoveSystems(new[] { system });
    }

    public void RemoveSystems(IEnumerable<SolarSystem> toRemove)
    {
        foreach(var system in toRemove)
        {
            this.systems.Remove(system);
        }
        foreach (var link in this.links
            .Where(l => toRemove.Contains(l.from) || toRemove.Contains(l.to))
            .ToList()
            )
        {
            this.links.Remove(link);
        }
    }
}
