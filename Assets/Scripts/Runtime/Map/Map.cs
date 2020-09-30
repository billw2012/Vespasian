using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

public class Body
{
    public GameObject prefab;
    public OrbitParameters parameters = OrbitParameters.Zero;

    public List<Body> children = new List<Body>();

    public int randomKey;
    public float temp;
    public float radius;
    public float mass;

    public GameObject InstanceHierarchy(float systemDanger)
    {
        var self = this.Instance(systemDanger);
        foreach (var child in this.children)
        {
            var childInstance = child.InstanceHierarchy(systemDanger);
            childInstance.transform.SetParent(self.GetComponent<Orbit>().position.transform, worldPositionStays: false);
        }
        return self;
    }

    GameObject Instance(float systemDanger)
    {
        var obj = Object.Instantiate(this.prefab);
        obj.GetComponent<BodyGenerator>().Init(this, systemDanger);
        return obj;
    }
}

public class Belt
{
    public GameObject prefab;
    public float radius;
    public float width;
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

    public Body main;

    public string name;

    public float danger;

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

        var rootBody = this.main.InstanceHierarchy(this.danger);
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
