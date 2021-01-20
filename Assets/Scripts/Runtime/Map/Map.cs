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
    private static int NextId = 0;
    public string specId;
    public int randomKey;
    public BodyRef bodyRef;

    public Dictionary<string, SaveData> savedComponents;

    public virtual string Name => this.bodyRef.ToString();

    private IEnumerable<(ISavable component, string key)> savables;
    private GameObject activeInstance;

    public Body() {}

    public Body(int systemId)
    {
        this.bodyRef = new BodyRef(systemId, NextId++);
    }

    /// <summary>
    /// This is called by the Map system when loading a system.
    /// </summary>
    /// <param name="bodySpecs"></param>
    /// <param name="systemDanger"></param>
    /// <returns></returns>
    public GameObject Instance(BodySpecs bodySpecs, SolarSystem solarSystem)
    {
        this.activeInstance = this.InstanceInternal(bodySpecs, solarSystem);
        Assert.IsTrue(activeInstance.transform.position.z == 0, $"New body {activeInstance.gameObject} isn't at z = 0");
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
    public void Unloading()
    {
        this.SaveComponents();
        this.activeInstance = null;
        this.savables = null;
    }

    protected virtual GameObject InstanceInternal(BodySpecs bodySpecs, SolarSystem solarSystem)
    {
        var spec = bodySpecs.GetSpecById(this.specId);
        Assert.IsNotNull(spec, $"Spec {this.specId} not found");
        var obj = Object.Instantiate(spec.prefab);
        Assert.IsNotNull(obj, $"Spec {spec.name}.prefab couldn't bin instantiated");
        // Need to do this before any further initialization occurs to ensure we don't capture a bunch of child objects that aren't part of the prefab
        this.savables = obj.GetComponentsInChildren<ISavable>()
            .Select(c => (c, GetFullKey(c as MonoBehaviour)))
            .ToList();

        var rng = new RandomX(this.randomKey);
        this.Apply(obj, rng);
        obj.GetComponent<BodyGenerator>()?.Init(this, rng, solarSystem);

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
                    SaveData.LoadObject(savable, data);
                }
            }
        }
    }

    public void SaveComponents()
    {
        this.savedComponents = new Dictionary<string, SaveData>();

        foreach (var (savable, key) in this.savables)
        {
            this.savedComponents.Add(key, SaveData.SaveObject(savable));
        }
    }

    private static string GetFullKey(MonoBehaviour component)
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

    public class DataEntry
    {
        public DataMask mask;
        public string name;
        public object entry;
        public string units;

        public DataEntry(DataMask mask, string name, object entry, string units = "")
        {
            this.mask = mask;
            this.name = name;
            this.entry = entry;
            this.units = units;
        }
    }

    public virtual ICollection<DataEntry> GetData(DataMask mask, BodySpecs specs) => Enumerable.Empty<DataEntry>().ToList();

    public virtual int GetDataCreditValue(DataMask data) => 0;
}

public abstract class OrbitingBody : Body
{
    public OrbitParameters parameters = OrbitParameters.Zero;
    public List<OrbitingBody> children = new List<OrbitingBody>();

    public OrbitingBody() { }
    public OrbitingBody(int systemId) : base(systemId) { }

    public override ICollection<DataEntry> GetData(DataMask mask, BodySpecs specs)
    {
        // TODO: cache all this in a dict instead? Maybe Lazy<>?
        var result = base.GetData(mask, specs);

        if (mask.HasFlag(DataMask.Orbit))
        {
            // TODO: localize later
            result.Add(new DataEntry(DataMask.Orbit, "Semi Major Axis", this.parameters.semiMajorAxis));
            result.Add(new DataEntry(DataMask.Orbit, "Eccentricity", this.parameters.eccentricity));
        }
        
        return result;
    }

    protected override void ApplyInternal(GameObject target, RandomX rng)
    {
        // Orbit setup
        var orbit = target.GetComponent<Orbit>();
        if (orbit != null)
        {
            orbit.parameters = this.parameters;
        }
    }
    
    public IEnumerable<OrbitingBody> ChildrenRecursive()
    {
        foreach (var child in this.children)
        {
            yield return child;
            foreach (var gchild in child.ChildrenRecursive())
            {
                yield return gchild;
            }
        }
    }
    
    protected override GameObject InstanceInternal(BodySpecs bodySpecs, SolarSystem solarSystem)
    {
        var self = base.InstanceInternal(bodySpecs, solarSystem);
        foreach (var child in this.children)
        {
            var childInstance = child.Instance(bodySpecs, solarSystem);
            childInstance.transform.SetParent(self.GetComponent<Orbit>().position.transform, worldPositionStays: false);
            Assert.IsTrue(childInstance.transform.position.z == 0, $"New body {childInstance.gameObject} isn't at z = 0");
        }
        return self;
    }
}

[RegisterSavableType]
public class StarOrPlanet : OrbitingBody
{
    public float temp;
    public float density;
    public float radius;
    public float mass;

    public float resources;
    public float habitability;

    public StarOrPlanet() { }
    public StarOrPlanet(int systemId) : base(systemId) { }
    
    public override ICollection<DataEntry> GetData(DataMask mask, BodySpecs specs)
    {
        // TODO: cache all this in a dict instead? Maybe Lazy<>?
        var result = base.GetData(mask, specs);

        if (mask.HasFlag(DataMask.Basic))
        {
            var spec = specs.GetSpecById(this.specId);

            result.Add(new DataEntry(DataMask.Basic, "Type", spec.name));
            result.Add(new DataEntry(DataMask.Basic, "Description", spec.description));
            result.Add(new DataEntry(DataMask.Basic, "Temp", this.temp, "K"));
            result.Add(new DataEntry(DataMask.Basic, "Radius", this.radius));
        }
        
        if (mask.HasFlag(DataMask.Composition))
        {
            result.Add(new DataEntry(DataMask.Composition, "Density", this.density));
            result.Add(new DataEntry(DataMask.Composition, "Mass", this.mass));
        }
        
        if (mask.HasFlag(DataMask.Habitability))
        {
            result.Add(new DataEntry(DataMask.Habitability, "Resources", this.resources));
        }        
        
        if (mask.HasFlag(DataMask.Habitability))
        {
            result.Add(new DataEntry(DataMask.Habitability, "Habitability", this.habitability));
        }

        return result;
    }

    public override int GetDataCreditValue(DataMask data)
    {
        int value = 0;
        for (int i = 1; i <= (int) DataMask.Count; i++)
        {
            switch(data & (DataMask)(1 << i))
            {
                case DataMask.Orbit:
                    value += 8;
                    break;
                case DataMask.Basic:
                    value += 16;
                    break;
                case DataMask.Composition:
                    value += 16;
                    break;
                case DataMask.Resources:
                    value += Mathf.CeilToInt(64 * this.resources);
                    break;
                case DataMask.Habitability:
                    value += Mathf.CeilToInt(128 * this.habitability);
                    break;
            }
        }
        return value;
    }
    
    protected override void ApplyInternal(GameObject target, RandomX rng)
    {
        base.ApplyInternal(target, rng);

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

[RegisterSavableType]
public class Station : OrbitingBody
{
    public Station() { }
    public Station(int systemId) : base(systemId) { }
}

[RegisterSavableType]
public class Belt : Body
{
    public float radius;
    public float width;
    public OrbitParameters.OrbitDirection direction;

    public Belt() { }
    public Belt(int systemId) : base(systemId) { }
    
    protected override void ApplyInternal(GameObject target, RandomX rng)
    {
        var asteroidRing = target.GetComponent<AsteroidRing>();
        asteroidRing.radius = this.radius;
        asteroidRing.width = this.width;
        asteroidRing.direction = this.direction;
    }
}

[RegisterSavableType]
public class Comet : OrbitingBody
{
    public Comet() { }
    public Comet(int systemId) : base(systemId) { }
}

[RegisterSavableType]
public class Link : IEquatable<Link>
{
    public SolarSystem from;
    public SolarSystem to;

    public bool Match(SolarSystem from, SolarSystem to) =>
        this.from == from && this.to == to ||
        this.from == to && this.to == from;

    public override bool Equals(object obj) => this.Match(((Link)obj).from, ((Link)obj).to);

    public bool Equals(Link other) => other != null && this.Match(other.from, other.to);

    public override int GetHashCode()
    {
        int hashCode = -1951484959;
        hashCode = hashCode * -1521134295 + EqualityComparer<SolarSystem>.Default.GetHashCode(this.from) + EqualityComparer<SolarSystem>.Default.GetHashCode(this.to);
        return hashCode;
    }

    public static bool operator ==(Link left, Link right) => EqualityComparer<Link>.Default.Equals(left, right);
    public static bool operator !=(Link left, Link right) => !(left == right);
}

/// <summary>
/// References a body by system and body ids
/// </summary>
public class BodyRef 
{
    public int systemId;
    public int bodyId;

    public BodyRef()
    {
        this.systemId = -1;
        this.bodyId = -1;
    }
    
    public BodyRef(int systemId, int bodyId)
    {
        this.systemId = systemId;
        this.bodyId = bodyId;
    }

    // BodyRef points to the system, not to a body within system
    public BodyRef(int systemId)
    {
        this.systemId = systemId;
        this.bodyId = -1;
    }

    public BodyRef(BodyRef other)
    {
        this.systemId = other.systemId;
        this.bodyId = other.bodyId;
    }
        
    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj))
        {
            return false;
        }

        if (ReferenceEquals(this, obj))
        {
            return true;
        }

        if (obj.GetType() != this.GetType())
        {
            return false;
        }

        return Equals((BodyRef) obj);
    }

    public bool EqualsSystem(BodyRef other)
    {
        return this.systemId == other.systemId;
    }

    private bool Equals(BodyRef other) => this.systemId == other.systemId && this.bodyId == other.bodyId;

    public override int GetHashCode()
    {
        unchecked
        {
            return (this.systemId * 397) ^ this.bodyId;
        }
    }

    public static bool operator ==(BodyRef left, BodyRef right) => Equals(left, right);

    public static bool operator !=(BodyRef left, BodyRef right) => !Equals(left, right);

    public override string ToString() => $"{this.systemId}:{this.bodyId}";
}

public class SolarSystem
{
    public int id;

    /// <summary>
    /// Primary direction the bodies orbit, rarely a body will orbit in the opposite direction.
    /// </summary>
    public OrbitParameters.OrbitDirection direction;

    public Vector2 position;

    public StarOrPlanet main;

    public float size;

    public string name;

    public float danger;

    public List<Comet> comets = new List<Comet>();
    public List<Belt> belts = new List<Belt>();

    private GameObject systemRoot;

    public IEnumerable<Body> AllBodies()
    {
        yield return this.main;
        foreach (var child in this.main.ChildrenRecursive())
        {
            yield return child;
        }

        foreach (var comet in this.comets)
        {
            yield return comet;
        }
        
        foreach (var belt in this.belts)
        {
            yield return belt;
        }
    }

    public SolarSystem() { }

    public SolarSystem(int id)
    {
        this.id = id;
    }
    
    public void Unload()
    {
        Assert.IsNotNull(this.systemRoot, $"System {this} isn't loaded, so can't be unloaded");
        foreach (var body in this.AllBodies().ToList())
        {
            body.Unloading();
        }
        // Deactivate the old system immediately so it is ignored from now on
        this.systemRoot.SetActive(false);
        Object.Destroy(this.systemRoot);
        this.systemRoot = null;
    }

    public void SaveAll()
    {
        foreach (var body in this.AllBodies())
        {
            body.SaveComponents();
        }
    }

    public async Task LoadAsync(SolarSystem current, BodySpecs bodySpecs, GameObject root)
    {
        Assert.IsNull(this.systemRoot, $"System {this} is already loaded");
        var rootBody = this.main.Instance(bodySpecs, this);
        Assert.IsTrue(rootBody.transform.position.z == 0, $"New body {rootBody.gameObject} isn't at z = 0");

        foreach (var belt in this.belts)
        {
            var beltObject = belt.Instance(bodySpecs, this);
            beltObject.transform.SetParent(rootBody.transform, worldPositionStays: false);
        }
        foreach (var comet in this.comets)
        {
            var cometObject = comet.Instance(bodySpecs, this);
            cometObject.transform.SetParent(rootBody.transform, worldPositionStays: false);
        }

        var rng = new RandomX();
        int enemyAICount = (int) rng.Range(0, this.danger * 4);
        for (int i = 0; i < enemyAICount; i++)
        {
            var enemySpec = bodySpecs.RandomAIShip(rng);
            var newEnemy = UnityEngine.Object.Instantiate(enemySpec.prefab, rootBody.transform);
            newEnemy.GetComponent<SimMovement>().SetPositionVelocity(
                Quaternion.Euler(0, 0, rng.Range(0, 360)) * Vector3.right * rng.Range(this.size * 0.25f, this.size * 1.25f),
                Quaternion.Euler(0, 0, rng.Range(0, 360)), 
                Vector2.right
                );
        }

        // var factions = Object.FindObjectsOfType<Faction>();
        // foreach (var faction in factions)
        // {
        //     foreach (var station in faction.stations.Where(s => s.systemId == this.id))
        //     {
        //         var stationObject = Object.Instantiate(faction.stationPrefab);
        //     }
        // }
        // We load the new system first and wait for it before unloading the previous one
        await new WaitUntil(() => rootBody.activeSelf);
        int beforeYieldFrame = Time.frameCount;
        await Task.Yield();
        await Task.Yield();
        Assert.IsFalse(beforeYieldFrame == Time.frameCount);

        current?.Unload();

        this.systemRoot = new GameObject("System");
        rootBody.transform.SetParent(this.systemRoot.transform, worldPositionStays: false);
        this.systemRoot.transform.SetParent(root.transform, worldPositionStays: false);
    }

    public static BodyGenerator FindBody(GameObject root, BodyRef bodyRef) => root.GetComponentsInChildren<BodyGenerator>().FirstOrDefault(b => b.BodyRef == bodyRef);
}

[RegisterSavableType]
public class Map : ISavable
{
    public List<Link> links = new List<Link>();

    public List<SolarSystem> systems = new List<SolarSystem>();

    public int NextSystemId => this.systems.Count;

    public void AddSystem(SolarSystem system)
    {
        Assert.AreEqual(system.id, NextSystemId);
        this.systems.Add(system);
    }
    
    public IEnumerable<(SolarSystem system, Link link)> GetConnected(SolarSystem system) =>
        this.links
            .Where(l => l.from == system || l.to == system)
            .Select(l => (system: l.from == system ? l.to : l.from, link: l));

    public void RemoveLink(Link link) => this.links.Remove(link);

    // TODO: optimize?
    public Body GetBody(BodyRef bodyRef) => this.systems[bodyRef.systemId].AllBodies().FirstOrDefault(b => b.bodyRef.bodyId == bodyRef.bodyId);

    public SolarSystem GetSystem(BodyRef bodyRef) => this.systems[bodyRef.systemId];
}
