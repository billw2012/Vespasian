using UnityEngine;

public static class MathX
{
    public static float NormalDistribution(float x, float mean, float stddev)
    {
        const float Sqrt2PI = 2.50662827463f;
        return Mathf.Exp(-0.5f * Mathf.Pow((x - mean) / stddev, 2)) / (stddev * Sqrt2PI);
    }

    // Log Normal Distribution
    // See https://www.desmos.com/calculator/pf7nrgjci8
    public class LogNormal
    {
        readonly float m;
        readonly float s;
        readonly float a;
        readonly float b;

        public LogNormal(float median, float spread)
        {
            this.m = median;
            this.s = median * spread;

            float logsm = Mathf.Log(this.s / this.m);
            this.a = 0.5f * Mathf.Sqrt(1f - 3f * logsm - Mathf.Sqrt(Mathf.Pow(logsm - 3f, 2) - 8f));
            this.b = Mathf.Log(this.m);
        }

        public float PDF(float x)
        {
            return Mathf.Exp(-Mathf.Pow(Mathf.Log(x) - this.b, 2) / (2 * this.a * this.a))
                / (x * this.a * Mathf.Sqrt(2 * Mathf.PI));
        }

        public float CDF(float x)
        {
            const float Sqrt2 = 1.41421356237f;
            return 0.5f * (1f + Erf((Mathf.Log(x) - this.b) / (this.a * Sqrt2)));
        }

        // Abramowitz and Stegun 7.1.26 formula for calculating Erf
        // Implementation from https://www.johndcook.com/blog/csharp_erf/
        static float Erf(float x)
        {
            const float a1 = 0.254829592f;
            const float a2 = -0.284496736f;
            const float a3 = 1.421413741f;
            const float a4 = -1.453152027f;
            const float a5 = 1.061405429f;
            const float p = 0.3275911f;

            float sign = Mathf.Sign(x);
            x = Mathf.Abs(x);

            // A&S formula 7.1.26
            float t = 1f / (1f + p * x);
            float y = 1f - ((((a5 * t + a4) * t + a3) * t + a2) * t + a1) * t * Mathf.Exp(-x * x);

            return sign * y;
        }
    }

    public static float RandomGaussian(float minValue = 0.0f, float maxValue = 1.0f)
    {
        float u, v, S;

        do
        {
            u = 2.0f * UnityEngine.Random.value - 1.0f;
            v = 2.0f * UnityEngine.Random.value - 1.0f;
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
}
