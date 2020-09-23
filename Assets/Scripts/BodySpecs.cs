using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

[CreateAssetMenu]
public class BodySpecs : ScriptableObject
{
    [Serializable]
    public class BodySpec
    {
        public GameObject prefab;

        [Tooltip("Probability of this body type appearing in a system"), Range(0.0001f, 1)]
        public float probability = 0.5f;

        [HideInInspector]
        public float sumWeight = 0;

        [Tooltip("Relative distance this body prefers to be at (0 indicates no preference)"), Range(0, 1)]
        public float distance;
        [Tooltip("Range around the preferred distance the body will appear in (normal distribution)"), Range(0, 1)]
        public float distanceRange;

        [Tooltip("Radius average"), Range(0, 20)]
        public float radius;
        [Tooltip("Radius range"), Range(0, 20)]
        public float radiusRange;

        [Tooltip("Density average"), Range(0, 1)]
        public float density;
        [Tooltip("Density range"), Range(0, 1)]
        public float densityRange;
    }

    [Serializable]
    public class SunSpec : BodySpec
    {
        [Tooltip("Brightness average"), Range(0, 10)]
        public float brightness;
        [Tooltip("Brightness range"), Range(0, 10)]
        public float brightnessRange;
    }

    [Serializable]
    public class PlanetSpec : BodySpec
    {
    }

    public List<SunSpec> suns;
    public List<PlanetSpec> planets;
    public List<PlanetSpec> moons;


    void NormalizeProbabilities<T>(List<T> specs) where T : BodySpec
    {
        float sum = specs.Select(s => s.probability).Sum();
        float total = 0;
        foreach(var s in specs)
        {
            total = s.sumWeight = total + s.probability / sum;
        }
    }

    void OnValidate()
    {
        void ValidateSpec(BodySpec spec)
        {
            spec.probability = float.IsNaN(spec.probability)? 0.5f : Mathf.Max(0.0001f, spec.probability);
        }
        void ValidateSpecs<T>(List<T> specs) where T : BodySpec
        {
            foreach(var spec in specs)
            {
                ValidateSpec(spec);
            }
        }

        ValidateSpecs(this.suns);
        ValidateSpecs(this.planets);
        ValidateSpecs(this.moons);

        this.NormalizeProbabilities(this.suns);
        this.NormalizeProbabilities(this.planets);
        this.NormalizeProbabilities(this.moons);
    }

    static T RandomSpec<T>(List<T> specs) where T : BodySpec
    {
        float r = Random.value;
        return specs.FirstOrDefault(s => s.sumWeight >= r);
    }

    public SunSpec RandomSun() => RandomSpec(this.suns);
    public PlanetSpec RandomPlanet() => RandomSpec(this.planets);
    public PlanetSpec RandomMoon() => RandomSpec(this.moons);
}
