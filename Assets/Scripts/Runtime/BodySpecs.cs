using System;
using System.Collections.Generic;
using System.Linq;
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
    public class StarSpec
    {
        public GameObject prefab;

        [Tooltip("Chance of this body type occurring"), Range(0, 1)]
        public float probability;

        public WeightedRandom massRandom = new WeightedRandom { min = 5, max = 30 };
        [Tooltip("Temperature in units of 10000 kelvin")]
        public WeightedRandom tempRandom = new WeightedRandom { min = 0.4f, max = 5f };
        public WeightedRandom densityRandom = new WeightedRandom { min = 1f, max = 1f, gaussian = true };
    }

    [Serializable]
    public class PlanetSpec
    {
        public GameObject prefab;

        [Tooltip("Higher priority will always replace lower priority")]
        public int priority = 0;

        [Tooltip("When planets have the same priority they are randomly selected based on this"), Range(0, 1)]
        public float probability = 1;

        public WeightedRandom densityRandom = new WeightedRandom { min = 2f, max = 2f, gaussian = true };

        public float minMass = 0;
        public float maxMass = Mathf.Infinity;

        public float minTemp = Mathf.NegativeInfinity;
        public float maxTemp = Mathf.Infinity;

        public bool Matches(float mass, float temp) => this.minMass <= mass && mass <= this.maxMass && this.minTemp <= temp && temp <= this.maxTemp;
    }

    //[Serializable]
    //public class PlanetSpec
    //{
    //    //[Tooltip("Mean relative distance this body can be at"), Range(0, 1)]
    //    //public float distanceMean;
    //    //// See https://www.desmos.com/calculator/2kmx0enkkz for normal distribution
    //    //[Tooltip("Standard deviation of the relative distance this body can appear in (as per normal distribution)"), Range(0, 1)]
    //    //public float distanceStdDev;

    //    [Range(0, 100)]
    //    public float mass = 20f;


    //    public GameObject[] variants;

    //    //public float CalcProbability(float temp, float mass)
    //    //{
    //    //    float tempP = MathX.NormalDistribution(temp, this.tempMean, this.tempStdDev);
    //    //    float massP = MathX.NormalDistribution(mass, this.massMean, this.massStdDev);
    //    //    return tempP * massP * this.probabilityModifier;
    //    //}
    //}

    //[Range(0, 1)]
    public float planetTempMultiplier = 1f;
    //[Range(0.0001f, 10)]
    public float planetMassMultiplier = 1f;
    //[Range(1, 5)]
    public float tempFalloffRate = 4f;

    public List<StarSpec> stars;
    public List<PlanetSpec> planets;

    public GameObject beltPrefab;
    public GameObject cometPrefab;

    //static BodySpec MatchSpec(IEnumerable<BodySpec> specs, Func<BodySpec, float> criteria, float value)
    //{
    //    return specs.OrderBy(s => Mathf.Abs(criteria(s) - value)).FirstOrDefault();
    //}

    //static BodySpec MatchSpec(List<BodySpec> specs, float temp, float mass, float radius)
    //{

    //}

    public StarSpec RandomStar()
    {
        return this.stars.SelectWeighted(Random.value, s => s.probability);
    }

    float Lum(float radius, float temp) => 4f * Mathf.PI * Mathf.Pow(radius, 2) * Mathf.Pow(temp, 4);

    float Power(float distance, float starLum) => starLum / (4f * Mathf.PI * Mathf.Pow(distance, this.tempFalloffRate));

    public float PlanetTemp(float distance, float starLum) => this.planetTempMultiplier * 2500f * Mathf.Pow(this.Power(distance, starLum), 0.25f);

    public float PlanetTemp(float distance, float starRadius, float starTemp) => this.PlanetTemp(distance, this.Lum(starRadius, starTemp));

    public PlanetSpec RandomPlanet(float mass, float temp)
    {
        var matches = this.planets.Where(p => p.Matches(mass * this.planetMassMultiplier, temp));
        if(!matches.Any())
        {
            Debug.LogWarning($"No planet found matching adjusted mass {mass * this.planetMassMultiplier} and temp {temp}");
            return this.planets.First();
        }
        int toppriority = matches.Max(p => p.priority);
        return matches
            .Where(p => p.priority == toppriority)
            .SelectWeighted(Random.value, p => p.probability);
    }
}
