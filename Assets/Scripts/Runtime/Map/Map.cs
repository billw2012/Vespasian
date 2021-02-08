using Pixelplacement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;
using Object = UnityEngine.Object;

public abstract class Body
{
    // ID starts from 1, as 0 is a reserved value.
    private static int NextId = 1;
    public string specId;
    public string name;
    public int randomKey;
    public BodyRef bodyRef;

    //public DictX<string, SaveData> savedComponents;
    public SaveData saveData;

    //private IEnumerable<(ISavable component, string key)> savables;
    private GameObject activeInstance;

    // Parameterless constructor required for serialization
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
        Assert.IsTrue(this.activeInstance.transform.position.z == 0, $"New body {this.activeInstance.gameObject} isn't at z = 0");
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
        if (this.saveData != null)
        {
            SaveData.LoadObject(target.GetComponent<SavableObject>(), this.saveData);
        }
    }

    /// <summary>
    /// This is called by the Map system when unloading a system
    /// </summary>
    public void Unloading()
    {
        this.Saving();
        this.activeInstance = null;
    }
    
    /// <summary>
    /// Call when saving the game to ensure latest state is serialized
    /// </summary>
    public void Saving()
    {
        var savableObject = this.activeInstance.GetComponent<SavableObject>();
        if (savableObject != null)
        {
            this.saveData = SaveData.SaveObject(savableObject);
        }
    }

    protected virtual GameObject InstanceInternal(BodySpecs bodySpecs, SolarSystem solarSystem)
    {
        var spec = bodySpecs.GetSpecById(this.specId);
        Assert.IsNotNull(spec, $"Spec {this.specId} not found");
        var obj = Object.Instantiate(spec.prefab);
        Assert.IsNotNull(obj, $"Spec {spec.name}.prefab couldn't bin instantiated");

        var rng = new RandomX(this.randomKey);
        this.Apply(obj, rng);
        obj.GetComponent<BodyGenerator>()?.Init(this, rng, solarSystem);

        return obj;
    }

    protected abstract void ApplyInternal(GameObject target, RandomX rng);

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

[RegisterSavableType]
public abstract class OrbitingBody : Body
{
    public OrbitParameters parameters = OrbitParameters.Zero;

    [RegisterSavableType]
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
    public BodyRef from;
    public BodyRef to;

    public bool Match(BodyRef from, BodyRef to) =>
        this.from == from && this.to == to ||
        this.from == to && this.to == from;

    public override bool Equals(object obj) => this.Match(((Link)obj).from, ((Link)obj).to);

    public bool Equals(Link other) => other != null && this.Match(other.from, other.to);

    public override int GetHashCode()
    {
        int hashCode = -1951484959;
        hashCode = hashCode * -1521134295 + EqualityComparer<BodyRef>.Default.GetHashCode(this.from) + EqualityComparer<BodyRef>.Default.GetHashCode(this.to);
        return hashCode;
    }

    public static bool operator ==(Link left, Link right) => EqualityComparer<Link>.Default.Equals(left, right);
    public static bool operator !=(Link left, Link right) => !(left == right);
}

/// <summary>
/// References a body by system and body ids
/// </summary>
[RegisterSavableType]
[Serializable]
public class BodyRef 
{
    // This type is immutable so setters are private
    public int systemId { get; private set; }
    public int bodyId { get; private set; }
    
    public bool isSystem => this.bodyId == -1;

    public BodyRef() {}
    
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

    public BodyRef SystemRef() => new BodyRef(this.systemId);

    public bool EqualsSystem(BodyRef other) => this.systemId == other.systemId;

    public bool Equals(BodyRef other) => this.systemId == other.systemId && this.bodyId == other.bodyId;
    public override bool Equals(object obj) => obj is BodyRef other && this.Equals(other);

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
    public BodyRef id;

    /// <summary>
    /// Primary direction the bodies orbit, rarely a body will orbit in the opposite direction.
    /// </summary>
    public OrbitParameters.OrbitDirection direction;

    public Vector2 position;

    public Color[] backgroundColors;

    public StarOrPlanet main;

    [IgnoreDataMember]
    public GameObject primary;

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
        this.id = new BodyRef(id);
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
            body.Saving();
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

        var factions = Object.FindObjectsOfType<Faction>();
        foreach (var faction in factions)
        {
            faction.SpawnSystem(this, rootBody);
        }

        // We load the new system first and wait for it before unloading the previous one
        await new WaitUntil(() => rootBody.activeSelf);
        await Awaiters.NextFrame;
        await Awaiters.NextFrame;
        current?.Unload();

        var fromColors = current?.backgroundColors ?? this.backgroundColors.Select(_ => Color.black);
        foreach (var (background, colors) in Object.FindObjectsOfType<Background>()
            .Where(b => b.colorationIndex != -1)
            .OrderBy(b => b.colorationIndex)
            .Zip(fromColors.Zip(this.backgroundColors, (from, to) => (from, to)),
                (background, colors) => (background, colors)))
        {
            Tween.Value(colors.from, colors.to, c => background.SetColor(c), 1, 0);
        }

        this.systemRoot = new GameObject("System");
        rootBody.transform.SetParent(this.systemRoot.transform, worldPositionStays: false);
        this.systemRoot.transform.SetParent(root.transform, worldPositionStays: false);

        this.primary = rootBody;
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
        Assert.AreEqual(system.id.systemId, this.NextSystemId);
        this.systems.Add(system);
    }
    
    public IEnumerable<(SolarSystem system, Link link)> GetConnected(SolarSystem system) =>
        this.links
            .Where(l => l.from == system.id || l.to == system.id)
            .Select(l => (system: this.GetSystem(l.from == system.id ? l.to : l.from), link: l));

    public void RemoveLink(Link link) => this.links.Remove(link);

    // TODO: optimize?
    public Body GetBody(BodyRef bodyRef) => this.systems[bodyRef.systemId].AllBodies().FirstOrDefault(b => b.bodyRef.bodyId == bodyRef.bodyId);

    public SolarSystem GetSystem(BodyRef bodyRef) => this.systems[bodyRef.systemId];
}
