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
    [SerializeField] private BodySpecs bodySpecs = null;

    [SerializeField] private float yieldInterval = 60;

    [Tooltip("This is the primary value to use for balancing")]
    [SerializeField] private Yields yieldMultipliers = new Yields(1, 1, 1);

    [Tooltip("How resource cap scales with number of stations")]
    [SerializeField] private float resourceExpansionMultiplier = 15;

    [Tooltip("The min rate we can acquire resources")]
    [SerializeField] private float minResourcePerTick = 1;

    // Expansion
    public class StationRef
    {
        public BodyRef parent;
        public string stationSpecId;
        public BodySpecs.StationType type;

        public StationRef() { }
        public StationRef(BodyRef parent, BodySpecs.StationSpec spec)
        {
            this.parent = parent;
            this.stationSpecId = spec.id;
            this.type = spec.stationType;
        }
    }
    [RegisterSavableType]
    [Saved] public List<StationRef> stations { get; private set; } = new List<StationRef>();

    public class ExpansionTarget
    {
        public BodyRef parent;
        public BodySpecs.StationType type;
        public float score;
    }
    [RegisterSavableType]
    [Saved] public List<ExpansionTarget> expansionTargets { get; private set; } = new List<ExpansionTarget>();
    
    // Yields
    // (Should yields be a separate component?
    // Stations + yields are pretty much coupled concepts so probably it isn't necessary)
    [Saved] private float timeUntilYieldUpdate = 0;

    // How much per tick in and out
    [Saved] public Yields yieldIn { get; private set; } = (0, 0, 0);
    [Saved] public Yields yieldOut {get; private set;} = (0, 0, 0);
    [Saved] public Yields yieldFulfillment {get; private set;} = (0, 0, 0);

    // How much resource has been accumulated in total since last expansion (we don't accumulate
    // energy and population in this manner)
    [Saved] public float resourceAccumulated {get; private set;} = 0;
    // How much resource is required for next expansion
    [Saved] public float resourceForExpansion {get; private set;} = 0;
    // How much resource was left over last tick and assigned to expansion pool
    [Saved] public float resourceExpansionGrowthLastTick {get; private set;} = 0;
    
    public bool canExpand => this.resourceAccumulated == this.resourceForExpansion;
    
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
        this.UpdateExpansionTargets();
    }

    private Yields GetFinalYields(StarOrPlanet body, BodySpecs.StationSpec station) =>
        // DOING: how do we handle multiple station specs of the same type?
        // e.g. If two different station specs can yield the same resource how do we generate missions for them?
        // How about instead we limit the types based on the body it will orbit?
        // e.g. special type of mine around lava planets, special type of collector around neutron stars etc.
        // Solve this later, for now just get it working...............
        station.baseYields + station.yieldMultipliers * this.yieldMultipliers * body.GetYields() ;

    private void UpdateYields()
    {
        this.timeUntilYieldUpdate -= Time.deltaTime * Simulation.globalTickStep;
        if (this.timeUntilYieldUpdate > 0 || this.mapComponent.map == null)
        {
            return;
        }   
        this.timeUntilYieldUpdate = this.yieldInterval;

        this.yieldIn = (0, 0, 0);
        this.yieldOut = (0, 0, 0);
    
        // TODO OPT: we could cache almost all of this calculation, only updating it when we actually expand
        foreach (var (body, stationSpec) in this.stations
            .Select(s => (this.mapComponent.map.GetBody(s.parent) as StarOrPlanet, this.bodySpecs.GetStationSpecById(s.stationSpecId)))
            )
        {
            this.yieldIn += this.GetFinalYields(body, stationSpec);
            this.yieldOut += stationSpec.uses * this.yieldMultipliers;
        }

        this.yieldFulfillment.resource = this.yieldOut.resource == 0 ? 1 : Mathf.Min(1, this.yieldIn.resource / this.yieldOut.resource);
        this.yieldFulfillment.energy = this.yieldOut.energy == 0 ? 1 : Mathf.Min(1, this.yieldIn.energy / this.yieldOut.energy);
        this.yieldFulfillment.pop = this.yieldOut.pop == 0 ? 1 : Mathf.Min(1, this.yieldIn.pop / this.yieldOut.pop);

        this.resourceForExpansion = this.stations.Count * this.yieldMultipliers.resource * this.resourceExpansionMultiplier;

        this.resourceExpansionGrowthLastTick = Mathf.Max(this.minResourcePerTick, this.yieldIn.resource - this.yieldOut.resource);
        this.resourceAccumulated = Mathf.Min(this.resourceForExpansion, this.resourceAccumulated + this.resourceExpansionGrowthLastTick);
        
        Debug.Log($"Faction yield update: in {this.yieldIn}, out {this.yieldOut}, res growth {this.resourceExpansionGrowthLastTick}, res accumulated {this.resourceAccumulated} / {this.resourceForExpansion}");
    }

    private void UpdateExpansionTargets()
    {
        if (this.mapComponent.map == null)
        {
            return;
        }
        
        // Can't expand at all until resources are at capacity
        if (!this.canExpand)
        {
            this.expansionTargets.Clear();
        }
        else
        {
            Debug.Log($"Updating Faction AI expansion targets...");

            // Currently the entire map is 1 galaxy unit, so we scale distance UP to make a 0-1 score sensible.
            // This scoring is totally subjective.
            const float DistanceScoringFactor = 20;
            float DistanceToScore(float distance) => 1 / (distance * DistanceScoringFactor + 1);
            // TODO OPT: all of this can be optimized easily...
            IEnumerable<(SolarSystem system, StarOrPlanet body, float yieldScore, float distanceScore)> GetCandidates(
                DataMask dataRequired, BodySpecs.StationType type, Yields yieldMask) =>
                this.faction.data.KnownBodies
                    // exclude bodies we don't know enough about
                    .Where(bodyRef => (this.faction.data.GetData(bodyRef) & dataRequired) == dataRequired)
                    // exclude bodies that have any existing station
                    .Where(bodyRef => !this.stations.Any(s => s.parent == bodyRef))
                    // exclude systems that already have stations of the same type
                    .Where(bodyRef =>
                        !this.stations.Any(s => s.type == type && s.parent.SystemRef() == bodyRef.SystemRef()))
                    // Get the actual system and body definitions
                    .Select(bodyRef => (
                        system: this.mapComponent.map.GetSystem(bodyRef),
                        body: this.mapComponent.map.GetBody(bodyRef) as StarOrPlanet))
                    // Score for yield and distance from the nearest station
                    .Select(sb => (
                        sb.system,
                        sb.body,
                        yieldScore: (sb.body.GetYields() * yieldMask).Sum(),
                        distanceScore: DistanceToScore(this.stations.Select(c => Vector2.Distance(this.mapComponent.map.GetSystem(c.parent.SystemRef()).position, sb.system.position)).Min())
                        ))
                    .Where(sbs => sbs.yieldScore > 0)
                ;

            this.expansionTargets = new[]
            {
                (data: DataMask.Basic, type: BodySpecs.StationType.CollectorStation, yieldMask: Yields.EnergyMask),
                (data: DataMask.Resources, type: BodySpecs.StationType.MiningStation, yieldMask: Yields.ResourceMask),
                (data: DataMask.Habitability, type: BodySpecs.StationType.HabitatStation, yieldMask: Yields.PopMask),
            }.SelectMany(x =>
            {
                var candidates = GetCandidates(x.data, x.type, x.yieldMask).ToList();
                // Select best candidate for distance, yield and combination of both
                return candidates
                    .OrderByDescending(y => y.yieldScore)
                    .Select(y => new ExpansionTarget {parent = y.body.bodyRef, type = x.type, score = y.yieldScore})
                    .Take(1)
                    .Concat(candidates
                        .OrderByDescending(y => y.distanceScore)
                        .Select(y => new ExpansionTarget {parent = y.body.bodyRef, type = x.type, score = y.distanceScore})
                        .Take(1))
                    .Concat(candidates
                        .OrderByDescending(y => (y.yieldScore + y.distanceScore) / 2)
                        .Select(y => new ExpansionTarget {parent = y.body.bodyRef, type = x.type, score = (y.yieldScore + y.distanceScore) / 2})
                        .Take(1))
                    ;
            }).ToList();

            if (this.expansionTargets.Any())
            {
                Debug.Log($"Faction AI found {this.expansionTargets.Count} suitable expansion candidates");
                // NotificationsUI.Add($"<style=faction>New station built at {body.name} in {system.name}</style>");
                // this.CreateStation(body);
            }
            else
            {
                Debug.Log($"Faction AI could not find any suitable expansion candidates");
            }
        }
    }

    private void OnMapGenerated()
    {
        // Reveal all station planets to the player
        var playerData = ComponentCache.FindObjectOfType<PlayerController>().GetComponent<DataCatalog>();
        foreach (var station in this.stations)
        {
            playerData.AddData(station.parent, DataMask.All);
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
        var spec = this.bodySpecs.RandomStation(this.rng, BodySpecs.StationType.HomeStation, this.faction.factionType);
        planet.children.Add(new Station(planet.bodyRef.systemId)
        {
            specId = spec.id,
            parameters = new OrbitParameters
            {
                periapsis = planet.radius + 5,
                apoapsis = planet.radius + 5,
            }
        });
        this.stations.Add(new StationRef(planet.bodyRef, spec));
    }
}