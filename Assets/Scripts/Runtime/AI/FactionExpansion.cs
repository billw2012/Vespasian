// unset

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

[RequireComponent(typeof(Faction))]
public class FactionExpansion : MonoBehaviour, ISavable
{
    [SerializeField] private string stationSpecId = String.Empty;
    [SerializeField] private WeightedRandom expansionInterval = new WeightedRandom { min = 300, max = 900, gaussian = true};
    [SerializeField] private float yieldInterval = 60;
    [SerializeField] private float yieldResourcesMultiplier = 1;
    [SerializeField] private float yieldEnergyMultiplier = 1;
    [SerializeField] private float yieldPopulationMultiplier = 1;

    // Expansion
    [RegisterSavableType]
    [Saved] public HashSet<BodyRef> stations { get; private set; }= new HashSet<BodyRef>();
    [Saved] private float timeUntilExpansion = 0;

    // Yields
    // (Should yields be a separate component?
    // Stations + yields are pretty much coupled concepts so probably it isn't necessary)
    [Saved] private float timeUntilYieldUpdate = 0;
    [Saved] public float resources = 0;
    [Saved] public float energy = 0;
    [Saved] public float population = 0;

    // What data the AI needs to know before about a planet before it can consider occupying it
    private const DataMask OccupationDataRequired = DataMask.Habitability | DataMask.Resources;
    
    private MapComponent mapComponent;
    
    private RandomX rng;

    private Faction faction;
    
    private void Awake()
    {
        this.faction = this.GetComponent<Faction>();

        ComponentCache.FindObjectOfType<SaveSystem>().RegisterForSaving(this);

        this.mapComponent = ComponentCache.FindObjectOfType<MapComponent>();
        this.mapComponent.mapGenerated.AddListener(this.OnMapGenerated);

        this.rng = new RandomX();
    }

    private void Update()
    {
        this.UpdateYields();
        this.UpdateExpansion();
    }

    private void UpdateYields()
    {
        this.timeUntilYieldUpdate -= Time.deltaTime;
        if (this.timeUntilYieldUpdate > 0 || this.mapComponent.map == null)
            return;
        this.timeUntilYieldUpdate = this.yieldInterval;

        Debug.Log($"Faction updating yields...");
        foreach (var stationBody in this.stations
            .Select(this.mapComponent.map.GetBody)
            .OfType<StarOrPlanet>())
        {
            this.resources += stationBody.resources * this.yieldResourcesMultiplier * this.yieldInterval;
            this.energy += stationBody.temp * this.yieldEnergyMultiplier * this.yieldInterval;
            this.population += stationBody.habitability * this.yieldPopulationMultiplier * this.yieldInterval;
        }
        Debug.Log($"Faction new yields r{this.resources} / e{this.energy} / p{this.population}");
    }

    private void UpdateExpansion()
    {
        this.timeUntilExpansion -= Time.deltaTime;
        if (this.timeUntilExpansion > 0 || this.mapComponent.map == null)
            return;
        this.timeUntilExpansion = this.expansionInterval.Evaluate(this.rng);

        Debug.Log($"Faction AI expanding...");
        // Build new stations every so often, at good locations near to existing stations
        var currentStationSystems = this.stations
            .Select(this.mapComponent.map.GetSystem)
            .ToDictionary(s => s.id, s => s);
        // Select candidates from known bodies
        var candidates = this.faction.data.KnownBodies
            // only include bodies we know enough about
            .Where(bodyRef => this.faction.data.HaveData(bodyRef, OccupationDataRequired))
            // exclude systems that already have stations
            .Where(bodyRef => !currentStationSystems.ContainsKey(bodyRef))
            // Get the actual system and body definitions
            .Select(bodyRef => (system: this.mapComponent.map.GetSystem(bodyRef),
                body: this.mapComponent.map.GetBody(bodyRef) as StarOrPlanet))
            // Exclude non-planets (they will be null), and low habitability
            .Where(systemBody => systemBody.body?.habitability >= 0.25f)
            // order ascending by distance to closest station
            .OrderBy(systemBody => currentStationSystems.Values
                .Select(c => Vector2.Distance(c.position, systemBody.system.position)).Min())
            .ToList();

        if (candidates.Any())
        {
            var (system, body) = candidates.First();

            Debug.Log($"Faction AI found suitable expansion candidate at {body.name} in {system.name}");
            NotificationsUI.Add($"<style=faction>New station built at {body.name} in {system.name}</style>");
            this.CreateStation(body);
        }
        else
        {
            Debug.Log($"Faction AI could not find any suitable expansion candidate, explore more systems!");
        }
    }

    private void OnMapGenerated()
    {
        // Reveal all station planets to the player
        var playerData = ComponentCache.FindObjectOfType<PlayerController>().GetComponent<DataCatalog>();
        foreach (var station in this.stations)
        {
            playerData.AddData(station, DataMask.All);
        }
    }

    /// <summary>
    /// Called from a background thread to populate the system map with faction stuff, during
    /// map generation.
    /// </summary>
    /// <param name="map"></param>
    public void PopulateMap(Map map)
    {
        var startingPlanet = map.systems
            .OrderBy(s => s.position.x)
            .Select(s => s.AllBodies()
                .OfType<StarOrPlanet>()
                .Where(p => p.habitability >= 0.25f)
                .SelectRandom())
            .Where(b => b != null)
            .Take(map.systems.Count / 10 + 1)
            .SelectRandom()
            ;

        if (startingPlanet == null)
        {
            Debug.LogError($"Couldn't find suitable home world for faction {this.faction.gameObject}");
            return;
        }
        
        // Always uniquely name the home planet 
        startingPlanet.ApplyUniqueName(force: true);
        
        // Update our station list on the main thread, wait for it so further processing can see it
        ThreadingX.RunOnUnityThread(() =>
        {
            this.CreateStation(startingPlanet);
        }).Wait();
    }

    private void CreateStation(StarOrPlanet planet)
    {
        planet.children.Add(new Station(planet.bodyRef.systemId)
        {
            specId = this.stationSpecId,
            parameters = new OrbitParameters
            {
                periapsis = planet.radius + 5,
                apoapsis = planet.radius + 5,
            }
        });
        this.stations.Add(planet.bodyRef);
    }
}