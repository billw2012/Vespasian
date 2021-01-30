using System;
using UnityEngine;
using UnityEngine.Assertions;

public static class MathX
{
    public static float NormalDistribution(float x, float mean, float stddev)
    {
        const float Sqrt2PI = 2.50662827463f;
        return Mathf.Exp(-0.5f * Mathf.Pow((x - mean) / stddev, 2)) / (stddev * Sqrt2PI);
    }
    
    public static float NormalDistributionFixedHeight(float x, float mean, float stddev)
    {
        return Mathf.Exp(-Mathf.Pow(x - mean, 2)) / (2 * stddev * stddev);
    }

    // Log Normal Distribution
    // See https://www.desmos.com/calculator/pf7nrgjci8
    public class LogNormal
    {
        private readonly float m;
        private readonly float s;
        private readonly float a;
        private readonly float b;

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
        private static float Erf(float x)
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

    public static float RectCircleOverlap(Rect r, Vector2 circlePos, float circleRadius) => RectCircleOverlap(r.xMin, r.xMax, r.yMin, r.yMax, circlePos.x, circlePos.y, circleRadius);

    // area of the intersection of a general box with a general circle
    // Converted from https://stackoverflow.com/a/32698993/6402065
    public static float RectCircleOverlap(float xMin, float xMax, float yMin, float yMax, float circleX, float circleY, float circleRadius) 
    {
        Assert.IsTrue(circleRadius >= 0);

        // returns the positive root of intersection of line y = h with circle centered at the origin and radius r
        float Section(float h)
        {
            // http://www.wolframalpha.com/input/?i=r+*+sin%28acos%28x+%2F+r%29%29+%3D+h
            return h < circleRadius ? Mathf.Sqrt(circleRadius * circleRadius - h * h) : 0; 
        }

        // indefinite integral of circle segment
        float g(float x, float h)
        {
            // http://www.wolframalpha.com/input/?i=r+*+sin%28acos%28x+%2F+r%29%29+-+h
            return .5f * (Mathf.Sqrt(1 - x * x / (circleRadius * circleRadius)) * x * circleRadius + circleRadius * circleRadius * Mathf.Asin(x / circleRadius) - 2 * h * x);
        }

        // area of intersection of an infinitely tall box with left edge at x0, right edge at x1, bottom edge at h and top edge at infinity, with circle centered at the origin with radius r
        float Area(float x0, float x1, float h) 
        {
            if (x0 > x1)
            {
                // this must be sorted otherwise we get negative area
                (x0, x1) = (x1, x0);
            }
            float s = Section(h);
            // integrate the area
            return g(Mathf.Clamp(x1, -s, s), h) - g(Mathf.Clamp(x0, -s, s), h);
        }

        // area of the intersection of a finite box with a circle centered at the origin with radius r
        float Area2(float x0, float x1, float y0, float y1) 
        {
            if (y0 > y1)
            {
                // this will simplify the reasoning
                (y0, y1) = (y1, y0);
            }
            if (y0 < 0)
            {
                if (y1 < 0)
                {
                    // the box is completely under, just flip it above and try again
                    return Area2(x0, x1, -y0, -y1);
                }
                else
                {
                    // the box is both above and below, divide it to two boxes and go again
                    return Area2(x0, x1, 0, -y0) + Area2(x0, x1, 0, y1);
                }
            }
            else
            {
                Assert.IsTrue(y1 >= 0); // y0 >= 0, which means that y1 >= 0 also (y1 >= y0) because of the swap at the beginning
                return Area(x0, x1, y0) - Area(x0, x1, y1); // area of the lower box minus area of the higher box
            }
        }

        // get rid of the circle center
        xMin -= circleX; xMax -= circleX;
        yMin -= circleY; yMax -= circleY;

        return Area2(xMin, xMax, yMin, yMax);
    }

    // As per: https://www.desmos.com/calculator/skjkarsia1
    public static float Sigmoid(float x, float a, float b, float k) => k / (1f + Mathf.Exp(a + b * x));
    
    // As per: https://en.wikipedia.org/wiki/Cubic_Hermite_spline
    public static float Hermite(float value1, float tangent1, float value2, float tangent2, float t)
    {
        float squared = t * t;
        float cubed = t * squared;
        float h00 = 2.0f * cubed - 3.0f * squared + 1.0f;
        float h10 = cubed - 2.0f * squared + t;
        float h01 = -2.0f * cubed + 3.0f * squared;
        float h11 = cubed - squared;
        
        return value1 * h00 + tangent1 * h10 + value2 * h01 + tangent2 * h11;
    }
    
    public static Vector2 Hermite(Vector2 value1, Vector2 tangent1, Vector2 value2, Vector2 tangent2, float t)
    {
        float squared = t * t;
        float cubed = t * squared;
        float h00 = 2.0f * cubed - 3.0f * squared + 1.0f;
        float h10 = cubed - 2.0f * squared + t;
        float h01 = -2.0f * cubed + 3.0f * squared;
        float h11 = cubed - squared;
        
        return value1 * h00 + tangent1 * h10 + value2 * h01 + tangent2 * h11;
    }
    
    public static Vector3 Hermite(Vector3 value1, Vector3 tangent1, Vector3 value2, Vector3 tangent2, float t)
    {
        float squared = t * t;
        float cubed = t * squared;
        float h00 = 2.0f * cubed - 3.0f * squared + 1.0f;
        float h10 = cubed - 2.0f * squared + t;
        float h01 = -2.0f * cubed + 3.0f * squared;
        float h11 = cubed - squared;
        
        return value1 * h00 + tangent1 * h10 + value2 * h01 + tangent2 * h11;
    }
    
    public static Vector4 Hermite(Vector4 value1, Vector4 tangent1, Vector4 value2, Vector4 tangent2, float t)
    {
        float squared = t * t;
        float cubed = t * squared;
        float h00 = 2.0f * cubed - 3.0f * squared + 1.0f;
        float h10 = cubed - 2.0f * squared + t;
        float h01 = -2.0f * cubed + 3.0f * squared;
        float h11 = cubed - squared;
        
        return value1 * h00 + tangent1 * h10 + value2 * h01 + tangent2 * h11;
    }
    
    // From https://rosettacode.org/wiki/Roots_of_a_function#Brent.27s_Method
    public static (bool found, double t) FindRoot(Func<double, double> f, double lower, double upper, double tolerance, uint maxIter = 50)
    {
        void Swap<T>(ref T _a, ref T _b)
        {
            var tmp = _a;
            _a = _b;
            _b = tmp;
        }
        
        double a = lower;
        double b = upper;
        double fa = f(a);
        double fb = f(b);

        Assert.IsTrue(Math.Sign(fa) != Math.Sign(fb), "Signs of f(lower) and f(upper) must be opposites");
        //if (Math.Sign(fa) == Math.Sign(fb))
        //throw new ArgumentException("Signs of f(lower_bound) and f(upper_bound) must be opposites");
 
        if (Math.Abs(fa) < Math.Abs(b)) // if magnitude of f(lower_bound) is less than magnitude of f(upper_bound)
        {
            Swap(ref a, ref b);
            Swap(ref fa, ref fb);
        }
 
        double c = a;      // c now equals the largest magnitude of the lower and upper bounds
        double fc = fa;    // precompute function evalutation for point c by assigning it the same value as fa
        bool mflag = true; // boolean flag used to evaluate if statement later on
        double s = 0;      // Our Root that will be returned
        double d = 0;      // Only used if mflag is unset (mflag == false)
 
        for (uint iter = 1; iter < maxIter; ++iter)
        {
            if (Math.Abs(b - a) < tolerance)
            {
                return (true, s);
            }
 
            if (fa != fc && fb != fc)
            {
                // use inverse quadratic interpolation
                s =   a * fb * fc / ((fa - fb) * (fa - fc))
                    + b * fa * fc / ((fb - fa) * (fb - fc))
                    + c * fa * fb / ((fc - fa) * (fc - fb));
            }
            else
            {
                // secant method
                s = b - fb * (b - a) / (fb - fa);
            }
 
            // checks to see whether we can use the faster converging quadratic && secant methods or if we need to use bisection
            if (    s < (3 * a + b) * 0.25 || 
                    s > b ||
                    mflag && Math.Abs(s - b) >= Math.Abs(b - c) * 0.5 ||
                    !mflag && Math.Abs(s - b) >= Math.Abs(c - d) * 0.5 ||
                    mflag && Math.Abs(b - c) < tolerance ||
                    !mflag && Math.Abs(c - d) < tolerance    )
            {
                // bisection method
                s = (a + b) * 0.5;
 
                mflag = true;
            }
            else
            {
                mflag = false;
            }
 
            double fs = f(s);
            d = c;    // first time d is being used (wasnt used on first iteration because mflag was set)
            c = b;    // set c equal to upper bound
            fc = fb;  // set f(c) = f(b)
 
            if (fa * fs < 0) // fa and fs have opposite signs
            {
                b = s;
                fb = fs;
            }
            else
            {
                a = s;
                fa = fs;
            }
 
            if (Math.Abs(fa) < Math.Abs(fb)) // if magnitude of fa is less than magnitude of fb
            {
                Swap(ref a, ref b);
                Swap(ref fa, ref fb); // make sure f(a) and f(b) are correct after swap
            }
        }

        return (false, 0);
        //throw new AggregateException("The solution does not converge or iterations are not sufficient");
    }

    public static float StoppingDistance(float currSpeed, float finalSpeed, float acceleration) => Mathf.Pow(currSpeed - finalSpeed, 2) / (2f * acceleration);

    public static float DistanceInTime(float currSpeed, float acceleration, float time)
    {
        return currSpeed * time + 0.5f * acceleration * time * time;
    }

    public static float DistanceInTime(float acceleration, float time) => 0.5f * acceleration * time * time;
    public static float TimeToCoverDistance(float acceleration, float distance) => Mathf.Sqrt(2f * distance / acceleration);

    public static float Remap(float fromMin, float fromMax, float toMin, float toMax, float value)
        => Mathf.Lerp(toMin, toMax, Mathf.InverseLerp(fromMin, fromMax, value));
}
