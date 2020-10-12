﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine;
using Random = UnityEngine.Random;

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
        public string id = Guid.NewGuid().ToString();
        public GameObject prefab;

        [Tooltip("Higher priority will always replace lower priority when all criteria match")]
        public int priority = 0;

        [Tooltip("Chance of this body type occurring"), Range(0, 1)]
        public float probability = 1;
    }

    [Serializable][KnownType(typeof(StarSpec))]
    public class StarSpec : BodySpec
    {
        public WeightedRandom massRandom = new WeightedRandom { min = 5, max = 30 };
        [Tooltip("Temperature in units of 10000 kelvin")]
        public WeightedRandom tempRandom = new WeightedRandom { min = 0.4f, max = 5f };
        public WeightedRandom densityRandom = new WeightedRandom { min = 1f, max = 1f, gaussian = true };
    }

    [Serializable][KnownType(typeof(PlanetSpec))]
    public class PlanetSpec : BodySpec
    {
        public WeightedRandom densityRandom = new WeightedRandom { min = 2f, max = 2f, gaussian = true };

        public float minMass = 0;
        public float maxMass = Mathf.Infinity;

        public float minTemp = Mathf.NegativeInfinity;
        public float maxTemp = Mathf.Infinity;

        public bool Matches(float mass, float temp) => this.minMass <= mass && mass <= this.maxMass && this.minTemp <= temp && temp <= this.maxTemp;
    }

    [Serializable][KnownType(typeof(BeltSpec))]
    public class BeltSpec : BodySpec
    {
        public float minDistance = 0f;
        public float maxDistance = Mathf.Infinity;

        public bool Matches(float distance) => this.minDistance <= distance && distance <= this.maxDistance;
    }

    [Serializable][KnownType(typeof(CometSpec))]
    public class CometSpec : BodySpec
    {
        public WeightedRandom relativePeriapsisRandom = new WeightedRandom { min = 0f, max = 0.1f };
        public WeightedRandom eccentricityRandom = new WeightedRandom { min = 0.6f, max = 0.95f };
        public float minApproach = 10f;
    }

    public List<StarSpec> stars;

    public float planetTempMultiplier = 1f;
    public float planetMassMultiplier = 1f;
    public float tempFalloffRate = 4f;
    public List<PlanetSpec> planets;

    public List<BeltSpec> belts;

    public List<CometSpec> comets;

    public IEnumerable<BodySpec> all => this.stars.OfType<BodySpec>().Concat(this.planets).Concat(this.belts).Concat(this.comets);

    public StarSpec RandomStar() => this.stars.SelectWeighted(Random.value, s => s.probability);

    public PlanetSpec RandomPlanet(float mass, float temp) => MatchedRandom(this.planets, p => p.Matches(mass * this.planetMassMultiplier, temp));

    public BeltSpec RandomBelt(float distance) => MatchedRandom(this.belts, b => b.Matches(distance));

    public CometSpec RandomComet() => MatchedRandom(this.comets, c => true);

    public BodySpec GetSpecById(string id) => this.all.FirstOrDefault(b => b.id == id);

    public float PlanetTemp(float distance, float starLum) => this.planetTempMultiplier * 2500f * Mathf.Pow(this.Power(distance, starLum), 0.25f);

    public float PlanetTemp(float distance, float starRadius, float starTemp) => this.PlanetTemp(distance, this.Lum(starRadius, starTemp));

    float Lum(float radius, float temp) => 4f * Mathf.PI * Mathf.Pow(radius, 2) * Mathf.Pow(temp, 4);

    float Power(float distance, float starLum) => starLum / (4f * Mathf.PI * Mathf.Pow(distance, this.tempFalloffRate));

    static T MatchedRandom<T>(IEnumerable<T> specs, Func<T, bool> matchFunc) where T : BodySpec
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
            .SelectWeighted(Random.value, p => p.probability);
    }
}
