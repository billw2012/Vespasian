using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    public class MathXTests
    {
        [Test]
        public void NormalDistribution()
        {
            // Pretty bad error margin for this function...
            const float Epsilon = 0.01f;

            // Test shape
            Assert.AreEqual(0.4f, MathX.NormalDistribution(0 + 0, 0, 1), Epsilon);
            Assert.AreEqual(0.242f, MathX.NormalDistribution(0 + 1, 0, 1), Epsilon);
            Assert.AreEqual(0.242f, MathX.NormalDistribution(0 - 1, 0, 1), Epsilon);
            Assert.AreEqual(0.054f, MathX.NormalDistribution(0 + 2, 0, 1), Epsilon);
            Assert.AreEqual(0.054f, MathX.NormalDistribution(0 - 2, 0, 1), Epsilon);

            // Test mean offset
            Assert.AreEqual(0.4f, MathX.NormalDistribution(1 + 0, 1, 1), Epsilon);
            Assert.AreEqual(0.242f, MathX.NormalDistribution(1 + 1, 1, 1), Epsilon);
            Assert.AreEqual(0.242f, MathX.NormalDistribution(1 - 1, 1, 1), Epsilon);
            Assert.AreEqual(0.054f, MathX.NormalDistribution(1 + 2, 1, 1), Epsilon);
            Assert.AreEqual(0.054f, MathX.NormalDistribution(1 - 2, 1, 1), Epsilon);

            // Test stddev
            Assert.AreEqual(0.2f, MathX.NormalDistribution(0 + 0, 0, 2), Epsilon);
            Assert.AreEqual(0.176f, MathX.NormalDistribution(0 + 1, 0, 2), Epsilon);
            Assert.AreEqual(0.176f, MathX.NormalDistribution(0 - 1, 0, 2), Epsilon);
            Assert.AreEqual(0.121f, MathX.NormalDistribution(0 + 2, 0, 2), Epsilon);
            Assert.AreEqual(0.121f, MathX.NormalDistribution(0 - 2, 0, 2), Epsilon);
            Assert.AreEqual(0.0648f, MathX.NormalDistribution(0 + 3, 0, 2), Epsilon);
            Assert.AreEqual(0.0648f, MathX.NormalDistribution(0 - 3, 0, 2), Epsilon);
        }

        [Test]
        public void LogNormal()
        {
            // Pretty good error margin for this function!
            const float Epsilon = 0.00001f;

            var tests = new[] {
                (mean: 1f, spread: 0.1f, results: new [] {
                    (x: 0.1f, pdf: 0.194632267689, cdf: 0.00640017441935),
                    (x: 0.3f, pdf: 0.6162675095, cdf: 0.0965283631472),
                    (x: 0.5f, pdf: 0.651425650297, cdf: 0.226823973292),
                    (x: 1.0f, pdf: 0.431288205504, cdf: 0.5),
                    (x: 1.5f, pdf: 0.261188015867, cdf: 0.669430015077),
                    (x: 2.0f, pdf: 0.162856412574, cdf: 0.773176026708),
                    (x: 3.0f, pdf: 0.0710130863862, cdf: 0.882521544518),
                }),
                (mean: 2f, spread: 0.9f, results: new [] {
                    (x: 0.1f, pdf: 0, cdf: 0),
                    (x: 0.5f, pdf: 0, cdf: 0),
                    (x: 1.0f, pdf: 0, cdf: 0),
                    (x: 1.5f, pdf: 0.0226980686307, cdf: 0.000929229998631),
                    (x: 1.6f, pdf: 0.146445056554, cdf: 0.00789258582583),
                    (x: 1.7f, pdf: 0.541311787574, cdf: 0.0393702300337),
                    (x: 2.0f, pdf: 2.15776972794, cdf: 0.5),
                    (x: 2.1f, pdf: 1.78783666072, cdf: 0.701175841935),
                    (x: 2.3f, pdf: 0.598361839117, cdf: 0.934716285454),
                    (x: 2.5f, pdf: 0.0937248361943, cdf: 0.992107414174),
                    (x: 3.0f, pdf: 0.0000956011360984, cdf: 0.999994225323),
                })
            };
            foreach (var t in tests)
            {
                var ln = new MathX.LogNormal(t.mean, t.spread);
                foreach (var v in t.results)
                {
                    Assert.AreEqual(v.pdf, ln.PDF(v.x), Epsilon);
                    Assert.AreEqual(v.cdf, ln.CDF(v.x), Epsilon);
                }
            }
        }

        //[Test]
        //public void RandomGaussian()
        //{
        //    const float Epsilon = 0.001f;

        //    float[] AverageBuckets(float min, float max, float[] sampleValues)
        //    {
        //        int Closest(float val, float[] vals)
        //        {
        //            if (val < vals[0])
        //                return 0;

        //            for (int i = 0; i < vals.Length - 1; i++)
        //            {
        //                if(val <= (vals[i] + vals[i+1]) * 0.5f)
        //                {
        //                    return i;
        //                }
        //            }

        //            return vals.Length - 1;
        //        }

        //        float[] buckets = new float[bucketIntervals.Length];
        //        for (int i = 0; i < 10000; i++)
        //        {
        //            float val = MathX.RandomGaussian(min, max) / 10000f;
        //            This isn't right anyway, probably need to integrate to test?
        //            buckets[Closest(val, sampleValues)]
        //        }
        //        return v;
        //    }

        //    Assert.AreEqual(0.4f, Average(-3, 3), Epsilon);
        //}
    }
}
