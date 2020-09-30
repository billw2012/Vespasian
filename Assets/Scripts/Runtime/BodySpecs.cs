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
    public class BodySpec
    {
        public GameObject prefab;

        [Tooltip("Modifies chance of this body appearing, mostly useful for making them rarer"), Range(0, 1)]
        public float probabilityModifier = 1;

        //[Tooltip("Mean relative distance this body can be at"), Range(0, 1)]
        //public float distanceMean;
        //// See https://www.desmos.com/calculator/2kmx0enkkz for normal distribution
        //[Tooltip("Standard deviation of the relative distance this body can appear in (as per normal distribution)"), Range(0, 1)]
        //public float distanceStdDev;

        [Tooltip("Mean surface temp this body can exist at (for stars it is in units of 10000k, for planets in °C) (See https://www.desmos.com/calculator/ei4udblphl for heat calculations)")]
        public float tempMean = 0.8f;

        // See https://www.desmos.com/calculator/2kmx0enkkz for normal distribution
        [Tooltip("Standard deviation of the surface temp this body can exist at (as per normal distribution)")]
        public float tempStdDev = 0.5f;

        [Tooltip("Mean mass this body can have in our non-specific mass units"), Range(0, 100)]
        public float massMean = 20f;
        [Tooltip("Standard deviation of the mass this body can have (as per normal distribution)"), Range(1, 100)]
        public float massStdDev = 5f;

        public float CalcProbability(float temp, float mass)
        {
            float tempP = MathX.NormalDistribution(temp, this.tempMean, this.tempStdDev);
            float massP = MathX.NormalDistribution(mass, this.massMean, this.massStdDev);
            return tempP * massP * this.probabilityModifier;
        }
    }

    public List<BodySpec> stars;
    public List<BodySpec> planets;

    public BodySpec RandomStar(float temp, float mass)
    {
        return this.stars.SelectWeighted(Random.value, s => s.CalcProbability(temp, mass));
    }

    static float Lum(float radius, float temp) => 4f * Mathf.PI * Mathf.Pow(radius, 2) * Mathf.Pow(temp, 4);

    public static float PlanetTemp(float distance, float starLum) => starLum / (4f * Mathf.PI * distance);
    public static float PlanetTemp(float distance, float starRadius, float starTemp) => Lum(starRadius, starTemp) / (4f * Mathf.PI * distance);

    public BodySpec RandomPlanet(float planetTemp, float mass)
    {
        // To select a body:
        // Evaluate the normal distributions for heat and mass for each body, combine into a single probability with the weighting then 
        // select from them
        return this.planets.SelectWeighted(Random.value, b => b.CalcProbability(planetTemp, mass));
    }


    //public SunSpec RandomSun() => RandomSpec(this.suns);
    //public PlanetSpec RandomPlanet() => RandomSpec(this.planets);
    //public PlanetSpec RandomMoon() => RandomSpec(this.moons);
}
