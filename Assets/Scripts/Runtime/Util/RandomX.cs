using UnityEngine;
using Random = System.Random;

public class RandomX
{
    private readonly Random random;

    public RandomX()
    {
        this.random = new Random();
    }

    public RandomX(int seed)
    {
        this.random = new Random(seed);
    }

    public float value => (float)this.random.NextDouble();

    public float Range(float min, float max) => this.value * (max - min) + min;

    public int Range(int min, int max) => this.random.Next(min, max);

    public bool Decide(float chanceTrue = 0.5f) => this.value >= chanceTrue;
    
    public float RandomGaussian(float minValue = 0.0f, float maxValue = 1.0f)
    {
        float u, v, S;

        do
        {
            u = 2.0f * this.value - 1.0f;
            v = 2.0f * this.value - 1.0f;
            S = u * u + v * v;
        }
        while (S >= 1.0f);

        // Standard Normal Distribution
        float std = u * Mathf.Sqrt(-2.0f * Mathf.Log(S) / S);

        // Normal Distribution centered between the min and max value
        // and clamped following the "three-sigma rule"
        float mean = (minValue + maxValue) / 2.0f;
        float sigma = (maxValue - mean) / 3.0f;
        return Mathf.Clamp(std * sigma + mean, minValue, maxValue);
    }

    public Color ColorHSV() => Color.HSVToRGB(this.value, this.value, this.value);
}
