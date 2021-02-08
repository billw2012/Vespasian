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

    public bool Decide(float chanceTrue = 0.5f) => this.value <= chanceTrue;
    
    public float RandomGaussian(float minValue = 0.0f, float maxValue = 1.0f)
    {
        Debug.Assert(minValue <= maxValue);
        
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

    /// <summary>
    /// Return a random value sampled from a slice of a gaussian distribution.
    /// </summary>
    /// <param name="minValue"></param>
    /// <param name="maxValue"></param>
    /// <param name="sliceStart">Start of the gaussian slice, assumes the gaussian is centered at 0 and falling within -1 to 1 range</param>
    /// <param name="sliceEnd">End of the gaussian slice, assumes the gaussian is centered at 0 and falling within -1 to 1 range</param>
    /// <returns></returns>
    public float RandomGaussianSlice(float minValue = 0.0f, float maxValue = 1.0f, float sliceStart = -1f,
        float sliceEnd = 1f)
    {
        Debug.Assert(sliceStart < sliceEnd);
        Debug.Assert(sliceStart >= -1);
        Debug.Assert(sliceEnd <= 1);
        
        // We just have to keep trying until a value falls within the requested range.
        // We cannot just clamp it because that results in all values that would have fallen outside the range, instead 
        // falling exactly at the min or max value. i.e. It would NOT be a correct distribution!
        for (;;)
        {
            float val = this.RandomGaussian(-1f, 1f);
            if (val >= sliceStart && val <= sliceEnd)
            {
                // Remap from slice range to min max value range
                return MathX.Remap(sliceStart, sliceEnd, minValue, maxValue, val);
            }
        }
    }

    public Color ColorHSV() => Color.HSVToRGB(this.value, this.value, this.value);
    public Color ColorHue(float saturation, float value) => Color.HSVToRGB(this.value, saturation, value);
    public Color ColorHS(float value) => Color.HSVToRGB(this.value, this.value, value);
}
