// unset

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEngine.Assertions;
using Object = UnityEngine.Object;

public class Spawn
{
    public int id;
    public string specId;
    
    public DictX<string, SaveData> savedComponents;
    
    private IEnumerable<ISavable> savables;
    //private GameObject activeInstance;

    // Parameterless constructor required for serialization
    public Spawn() { }

    public Spawn(int id, string specId)
    {
        this.id = id;
        this.specId = specId;
    }

    public GameObject Instance(BodySpecs bodySpecs, SolarSystem solarSystem, RandomX rng)
    {
        var shipSpec = bodySpecs.GetAIShipSpecById(this.specId);
        var shipInstance = Object.Instantiate(shipSpec.prefab);
        
        this.savables = shipInstance.GetComponents<ISavable>()
            .ToList();
        if (this.savedComponents == null)
        {
            shipInstance.GetComponent<SimMovement>().SetPositionVelocity(
                Quaternion.Euler(0, 0, rng.Range(0, 360)) * Vector3.right *
                rng.Range(solarSystem.size * 0.25f, solarSystem.size * 1.25f),
                Quaternion.Euler(0, 0, rng.Range(0, 360)),
                Vector2.right
            );
        }
        else
        {
            this.LoadComponents();
        }

        return shipInstance;
    }
    
    public void Unloading()
    {
        this.SaveComponents();
        //this.activeInstance = null;
        this.savables = null;
    }

    public void Saving() => this.SaveComponents();

    private void LoadComponents()
    {
        Assert.IsNotNull(this.savedComponents);
        
        foreach(var savable in this.savables)
        {
            if(this.savedComponents.TryGetValue(savable.GetType().Name, out var data))
            {
                SaveData.LoadObject(savable, data);
            }
        }
    }

    private void SaveComponents()
    {
        this.savedComponents = new DictX<string, SaveData>();
        
        foreach (var savable in this.savables)
        {
            this.savedComponents.Add(savable.GetType().Name, SaveData.SaveObject(savable));
        }
    }
}


[RequireComponent(typeof(Faction))]
public class FactionSpawns : MonoBehaviour, ISavable, IPreSave
{
    [SerializeField]
    private float spawnIntervalSeconds = 300;
    [SerializeField]
    private BodySpecs bodySpecs = null;
    [SerializeField]
    private bool onlySpawnInKnownSystems = false;

    [RegisterSavableType]
    public class Spawns
    {
        public BodyRef bodyRef;
        public List<Spawn> spawns;
    }

    // Doesn't serialize properly as a dictionary, and perf wise its fine
    [Saved, RegisterSavableType]
    private readonly List<Spawns> ships = new List<Spawns>();
    
    [Saved]
    private float timeUntilRespawnSeconds = 0;

    [Saved] 
    private int nextSpawnId = 0;

    [Saved]
    private BodyRef currentSystem;
    
    private MapComponent mapComponent;
    
    private RandomX rng;

    private Faction faction;
    
    private List<Spawn> activeSpawns;
    private Simulation simulation;

    private void Awake()
    {
        this.faction = this.GetComponent<Faction>();
        
        FindObjectOfType<SaveSystem>().RegisterForSaving($"{this.faction}.FactionSpawns", this);

        this.mapComponent = FindObjectOfType<MapComponent>();
        this.mapComponent.mapGenerated.AddListener(this.OnMapGenerated);
        this.simulation = FindObjectOfType<Simulation>();

        this.rng = new RandomX();
        this.activeSpawns = new List<Spawn>();
    }

    private void Update()
    {
        // TODO: make spawning more interesting, perhaps determine how many there should be
        // in a system then add them over time per system?
        this.timeUntilRespawnSeconds -= Time.deltaTime * this.simulation.tickStep;
        if (this.timeUntilRespawnSeconds <= 0 && this.mapComponent.map != null)
        {
            foreach (var system in this.mapComponent.map.systems)
            {
                this.PopulateSystem(system);
            }
            
            this.timeUntilRespawnSeconds = this.spawnIntervalSeconds;
        }
    }

    private void OnMapGenerated()
    {
        foreach (var system in this.mapComponent.map.systems)
        {
            this.PopulateSystem(system);
        }
    }
    
    private void PopulateSystem(SolarSystem system)
    {
        if (this.onlySpawnInKnownSystems && !this.faction.data.HaveData(system.main.bodyRef, DataMask.Orbit))
        {
            return;
        }

        var systemSpawns = this.ships.FirstOrDefault(s => s.bodyRef == system.id);
        if (systemSpawns == null)
        {
            systemSpawns = new Spawns
            {
                bodyRef = system.id,
                spawns = new List<Spawn>()
            };
            this.ships.Add(systemSpawns);
        }

        int enemyAICount = (int)this.rng.Range(0, system.danger * 4);
        while (systemSpawns.spawns.Count < enemyAICount)
        {
            var enemySpec = this.bodySpecs.RandomAIShip(this.rng, this.faction.factionType);
            systemSpawns.spawns.Add(new Spawn(this.nextSpawnId++, enemySpec.id));
        }
    }
    
    public void SpawnSystem(SolarSystem system, GameObject rootBody)
    {
        foreach (var oldSpawn in this.activeSpawns)
        {
            oldSpawn.Unloading();
        }
        this.activeSpawns.Clear();
        
        var systemSpawns = this.ships.FirstOrDefault(s => s.bodyRef == system.id);
        if (systemSpawns != null)
        {
            foreach (var shipSpawn in systemSpawns.spawns)
            {
                this.activeSpawns.Add(shipSpawn);

                var shipInstance = shipSpawn.Instance(this.bodySpecs, system, this.rng);
                shipInstance.GetComponent<HealthComponent>().onKilled.AddListener(() =>
                {
                    shipInstance.SetActive(false);
                    systemSpawns.spawns.Remove(shipSpawn);
                    this.activeSpawns.Remove(shipSpawn);
                });
                shipInstance.transform.SetParent(rootBody.transform, worldPositionStays: false);
            }
        }
        
        this.currentSystem = system.id;
    }

    public void PreSave()
    {
        foreach (var activeSpawn in this.activeSpawns)
        {
            activeSpawn.Saving();
        }
    }
}