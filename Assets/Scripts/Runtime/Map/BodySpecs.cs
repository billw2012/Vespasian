using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine;

[CreateAssetMenu]
public class BodySpecs : ScriptableObject
{
    // Construct systems based on a normal distribution of planet sizes, shifting the normal center further and closer to the star.
    // Binary is just an extension of this where another Sun exists as a planet.
    // Vary planet type by radius / mass, and distance from sun, from rocky and small, to gas, to brown dwarf, to twin star.

    // So we can generate the system just with mass / radius, then select body types to full fill the requirements.
    // NOTE: Orbit separation requirements depends on the body mass somewhat, lighter bodies can orbit more closely, without perturbing each other?

    // Mass distribution - log normal (https://www.desmos.com/calculator/ysabhcc42g)
    // Total system mass can determine star and planets

    [Serializable]
    public class BodySpec
    {
        public string name;

        public string id = Guid.NewGuid().ToString();
        
        public GameObject prefab;
        public GameObject uiPrefab;

        public string description;

        public float dataValueMultiplier = 1;

        [Tooltip("Higher priority will always replace lower priority when all criteria match")]
        public int priority = 0;

        [Tooltip("Chance of this body type occurring"), Range(0, 1)]
        public float probability = 1;
        
        public WeightedRandom energyRandom = new WeightedRandom { min = 0f, max = 0f };
        public WeightedRandom resourcesRandom = new WeightedRandom { min = 0f, max = 0f };
        public WeightedRandom habitabilityRandom = new WeightedRandom { min = 0f, max = 0f };
    }

    [Serializable, KnownType(typeof(StarSpec))]
    public class StarSpec : BodySpec
    {
        public WeightedRandom massRandom = new WeightedRandom { min = 5, max = 30 };
        [Tooltip("Temperature in kelvin")]
        public WeightedRandom tempRandom = new WeightedRandom { min = 2000f, max = 30000f };
        public WeightedRandom densityRandom = new WeightedRandom { min = 1f, max = 1f, gaussian = true };
    }

    [Serializable, KnownType(typeof(PlanetSpec))]
    public class PlanetSpec : BodySpec
    {
        public WeightedRandom densityRandom = new WeightedRandom { min = 2f, max = 2f, gaussian = true };

        // For a planet the mass and temp are determined by the generator, then the PlanetSpec to use is 
        // selected from those that fall within the correct ranges
        public float minMass = 0;
        public float maxMass = Mathf.Infinity;

        public float minTemp = Mathf.NegativeInfinity;
        public float maxTemp = Mathf.Infinity;

        public float uniqueNameProbability = 0f;

        // This allows a star to be spawned in place of a planet (for binary like systems, or super jupiter / brown dwarfs)
        public bool isStar = false;

        [Tooltip("Maps mass to temperature")]
        public WeightedMapping tempByMass = new WeightedMapping(5, 30, 2000, 30000);

        public bool Matches(float mass, float temp) => this.minMass <= mass && mass <= this.maxMass && this.minTemp <= temp && temp <= this.maxTemp;
    }

    [Serializable]
    public class WeightedMapping
    {
        public AnimationCurve mapping = AnimationCurve.Linear(0, 0, 1, 1);
        public float minX;
        public float maxX;
        public float minY;
        public float maxY;

        public WeightedMapping(float minX, float maxX, float minY, float maxY)
        {
            this.minX = minX;
            this.maxX = maxX;
            this.minY = minY;
            this.maxY = maxY;
        }
        
        public float Evaluate(float value) => Mathf.Lerp(this.minY, this.maxY, this.mapping.Evaluate(Mathf.InverseLerp(this.minX, this.maxX, value)));
        
    }

    [Serializable, KnownType(typeof(BeltSpec))]
    public class BeltSpec : BodySpec
    {
        public float minDistance = 0f;
        public float maxDistance = Mathf.Infinity;

        public bool Matches(float distance) => this.minDistance <= distance && distance <= this.maxDistance;
    }

    [Serializable, KnownType(typeof(CometSpec))]
    public class CometSpec : BodySpec
    {
        public WeightedRandom relativePeriapsisRandom = new WeightedRandom { min = 0f, max = 0.1f };
        public WeightedRandom eccentricityRandom = new WeightedRandom { min = 0.6f, max = 0.95f };
        public float minApproach = 10f;
    }

    [Flags]
    public enum StationType
    {
        HomeStation        = 1 << 0, // Home base, where player begins, provides small amounts of everything
        MiningStation      = 1 << 1, // Provides resources, uses energy and population
        CollectorStation   = 1 << 2, // Provides energy, uses population
        HabitatStation     = 1 << 3, // Provides population, uses energy
    }
    
    [Serializable, KnownType(typeof(StationSpec))]
    public class StationSpec : BodySpec
    {
        public StationType stationType;
        public Faction.FactionType factions;
        public Yields baseYields;
        public Yields yieldMultipliers;
        public Yields uses;
        // TODO: limit these by the type of body they can orbit
    }

    public List<StarSpec> stars;

    public float planetTempMultiplier = 1f;
    public float planetMassMultiplier = 1f;
    public float tempFalloffRate = 4f;
    public List<PlanetSpec> planets;

    public List<BeltSpec> belts;

    public List<CometSpec> comets;
    
    public List<StationSpec> stations;

    [Serializable]
    public class AIShip
    {
        public string name;
        public string id = Guid.NewGuid().ToString();

        public GameObject prefab;
        
        [Tooltip("Chance of this AI ship type occurring"), Range(0, 1)]
        public float probability = 1;

        public Faction.FactionType faction = Faction.FactionType.None;
    }
    
    public List<AIShip> aiShips;

    public IEnumerable<BodySpec> all => this.stars.OfType<BodySpec>().Concat(this.planets).Concat(this.belts).Concat(this.comets).Concat(this.stations);

    public StarSpec RandomStar(RandomX rng) => this.stars.SelectWeighted(rng.value, s => s.probability);

    public PlanetSpec RandomPlanet(RandomX rng, float mass, float temp) => MatchedRandom(rng, this.planets, p => p.Matches(mass * this.planetMassMultiplier, temp));

    public BeltSpec RandomBelt(RandomX rng, float distance) => MatchedRandom(rng, this.belts, b => b.Matches(distance));

    public CometSpec RandomComet(RandomX rng) => MatchedRandom(rng, this.comets, c => true);

    public AIShip RandomAIShip(RandomX rng, Faction.FactionType factions) => this.aiShips.Where(s =>
        (s.faction & factions) != Faction.FactionType.None).SelectWeighted(rng.value, s => s.probability);

    public StationSpec RandomStation(RandomX rng, StationType type, Faction.FactionType faction) => this.stations
        .Where(s => s.stationType == type && s.factions.HasFlag(faction)).SelectWeighted(rng.value, s => s.probability);
    
    public BodySpec GetSpecById(string id) => this.all.FirstOrDefault(b => b.id == id);
    public StationSpec GetStationSpecById(string id) => this.stations.FirstOrDefault(b => b.id == id);
    
    public AIShip GetAIShipSpecById(string id) => this.aiShips.FirstOrDefault(s => s.id == id);

    public float PlanetTemp(float distance, float starLum) => this.planetTempMultiplier * 2500f * Mathf.Pow(this.Power(distance, starLum), 0.25f);

    public float PlanetTemp(float distance, float starRadius, float starTemp) => this.PlanetTemp(distance, this.Lum(starRadius, starTemp));

    private float Lum(float radius, float temp) => 4f * Mathf.PI * Mathf.Pow(radius, 2) * Mathf.Pow(temp / 10000f, 4);

    private float Power(float distance, float starLum) => starLum / (4f * Mathf.PI * Mathf.Pow(distance, this.tempFalloffRate));

    private static T MatchedRandom<T>(RandomX rng, IEnumerable<T> specs, Func<T, bool> matchFunc) where T : BodySpec
    {
        var matches = specs.Where(matchFunc);
        if (!matches.Any())
        {
            Debug.LogWarning($"No body found matching criteria");
            return specs.First();
        }
        int toppriority = matches.Max(p => p.priority);
        return matches
            .Where(p => p.priority == toppriority)
            .SelectWeighted(rng.value, p => p.probability);
    }
}
