using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;


public interface ISaved
{
}

public interface ISerializer
{
    void Add(string key, object value);
}

public interface IDeserializer
{
    T Get<T>(string key);
}


public interface ISavedCustomSerialization
{
    void Serialize(ISerializer serializer);
    void Deserialize(IDeserializer deserializer);
}

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class SavedAttribute : Attribute {}

public class SaveData : ISerializer, IDeserializer
{
    public Dictionary<string, object> data = new Dictionary<string, object>();

    public void Add(string key, object value) => this.data.Add(key, value);

    public T Get<T>(string key) => (T)this.data[key];
    public object Get(string key) => this.data[key];
}

public static class Save
{
    static void ForEachField(object obj, Action<FieldInfo> op)
    {
        const BindingFlags flag = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        foreach (var field in obj.GetType()
            .GetFields(flag)
            .Where(f => f.CustomAttributes.Any(a => a.AttributeType == typeof(SavedAttribute))))
        {
            op(field);
        }
    }
    static void ForEachProperty(object obj, Action<PropertyInfo> op)
    {
        const BindingFlags flag = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        foreach (var property in obj.GetType()
            .GetProperties(flag)
            .Where(f => f.CustomAttributes.Any(a => a.AttributeType == typeof(SavedAttribute))))
        {
            op(property);
        }
    }

    public static SaveData SaveObject(object obj)
    {
        var data = new SaveData();
        if(obj is ISaved)
        {
            ForEachField(obj, f => data.Add(f.Name, f.GetValue(obj)));
            ForEachProperty(obj, f => data.data.Add(f.Name, f.GetValue(obj)));
        }

        if(obj is ISavedCustomSerialization)
        {
            (obj as ISavedCustomSerialization).Serialize(data);
        }
        return data;
    }

    public static void LoadObject(object obj, SaveData data)
    {
        if (obj is ISaved)
        {
            ForEachField(obj, f => f.SetValue(obj, data.Get(f.Name)));
            ForEachProperty(obj, f => f.SetValue(obj, data.Get(f.Name)));
        }

        if (obj is ISavedCustomSerialization)
        {
            (obj as ISavedCustomSerialization).Deserialize(data);
        }
    }
}

public abstract class Body
{
    public string specId;
    public int randomKey;

    public Dictionary<string, SaveData> savedComponents;

    IEnumerable<(ISaved component, string key)> savables;
    GameObject activeInstance;

    /// <summary>
    /// This is called by the Map system when loading a system.
    /// </summary>
    /// <param name="bodySpecs"></param>
    /// <param name="systemDanger"></param>
    /// <returns></returns>
    public GameObject Instance(BodySpecs bodySpecs, float systemDanger)
    {
        this.activeInstance = this.InstanceInternal(bodySpecs, systemDanger);
        return this.activeInstance;
    }

    /// <summary>
    /// This is called by the BodyGenerator init function, with an rng it supplies.
    /// </summary>
    /// <param name="target"></param>
    /// <param name="rng"></param>
    public void Apply(GameObject target, RandomX rng)
    {
        this.ApplyInternal(target, rng);
        this.LoadComponents();
    }

    /// <summary>
    /// This is called by the Map system when unloading a system
    /// </summary>
    public virtual void Unloading()
    {
        this.SaveComponents();
        this.activeInstance = null;
        this.savables = null;
    }

    protected virtual GameObject InstanceInternal(BodySpecs bodySpecs, float systemDanger)
    {
        var spec = bodySpecs.GetSpecById(this.specId);
        var obj = Object.Instantiate(spec.prefab);

        // Need to do this before any further initialization occurs to ensure we don't capture a bunch of child objects that aren't part of the prefab
        this.savables = obj.GetComponentsInChildren<ISaved>()
            .Select(c => (c, GetFullKey(c as MonoBehaviour)))
            .ToList();

        var rng = new RandomX(this.randomKey);
        this.Apply(obj, rng);
        obj.GetComponent<BodyGenerator>().Init(this, rng, systemDanger);

        return obj;
    }

    protected abstract void ApplyInternal(GameObject target, RandomX rng);

    public void LoadComponents()
    {
        if(this.savedComponents != null)
        {
            foreach(var (savable, key) in this.savables)
            {
                if(this.savedComponents.TryGetValue(key, out var data))
                {
                    Save.LoadObject(savable, data);
                }
            }
        }
    }

    public void SaveComponents()
    {
        this.savedComponents = new Dictionary<string, SaveData>();

        foreach (var (savable, key) in this.savables)
        {
            this.savedComponents.Add(key, Save.SaveObject(savable));
        }
    }

    static string GetFullKey(MonoBehaviour component)
    {
        IList<string> names = new List<string> { component.ToString() };
        var obj = component.transform;
        while (obj != null)
        {
            names.Add(obj.gameObject.ToString());
            obj = obj.transform.parent;
        }
        return string.Join("/", names.Reverse());
    }
}

public class StarOrPlanet : Body
{
    public OrbitParameters parameters = OrbitParameters.Zero;

    public List<StarOrPlanet> children = new List<StarOrPlanet>();

    public float temp;
    public float density;
    public float radius;
    public float mass;

    protected override GameObject InstanceInternal(BodySpecs bodySpecs, float systemDanger)
    {
        var self = base.InstanceInternal(bodySpecs, systemDanger);
        foreach (var child in this.children)
        {
            var childInstance = child.Instance(bodySpecs, systemDanger);
            childInstance.transform.SetParent(self.GetComponent<Orbit>().position.transform, worldPositionStays: false);
        }
        return self;
    }

    public override void Unloading()
    {
        foreach(var child in this.children)
        {
            child.Unloading();
        }
        base.Unloading();
    }

    protected override void ApplyInternal(GameObject target, RandomX rng)
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
            bodyLogic.dayPeriod = rng.RandomGaussian(5, 30 * this.mass) * Mathf.Sign(rng.value - 0.5f);
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
}

public class Belt : Body
{
    public float radius;
    public float width;
    public OrbitParameters.OrbitDirection direction;

    protected override void ApplyInternal(GameObject target, RandomX rng)
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

    protected override void ApplyInternal(GameObject target, RandomX rng)
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
        hashCode = hashCode * -1521134295 + EqualityComparer<SolarSystem>.Default.GetHashCode(this.from) + EqualityComparer<SolarSystem>.Default.GetHashCode(this.to);
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

    public void Unload(GameObject root)
    {
        var systemObjectTransform = root.transform.Find("System");
        if (systemObjectTransform != null)
        {
            foreach (var body in this.main
                .Yield<Body>()
                .Concat(this.belts)
                .Concat(this.comets))
            {
                body.Unloading();
            }
            Object.Destroy(systemObjectTransform.gameObject);
        }
    }

    public async Task LoadAsync(SolarSystem current, BodySpecs bodySpecs, GameObject root)
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
        int beforeYieldFrame = Time.frameCount;
        await Task.Yield();
        await Task.Yield();
        Assert.IsFalse(beforeYieldFrame == Time.frameCount);

        if (current != null)
        {
            current.Unload(root);
        }

        var systemObject = new GameObject("System");
        systemObject.transform.SetParent(root.transform, worldPositionStays: false);

        rootBody.transform.SetParent(systemObject.transform, worldPositionStays: false);
    }
}

public class Map
{
    public List<Link> links = new List<Link>();
    public List<SolarSystem> systems = new List<SolarSystem>();

    public IEnumerable<(SolarSystem system, Link link)> GetConnected(SolarSystem system)
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
