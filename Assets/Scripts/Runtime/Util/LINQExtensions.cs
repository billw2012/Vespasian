using System;
using System.Collections.Generic;
using System.Linq;

public static class LINQExtensions
{
    public static T SelectWeighted<T>(this IEnumerable<T> @this, float rnd, Func<T, float> weightFn)
    {
        var prob = @this.Select(o => (obj: o, P: weightFn(o)));
        float totalProb = prob.Select(o => o.P).Sum();

        float randomP = rnd * totalProb;
        float sum = 0;
        foreach (var o in prob)
        {
            sum += o.P;
            if (sum >= randomP)
            {
                return o.obj;
            }
        }

        return prob.LastOrDefault().obj;
    }

    public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> @this)
    {
        return @this.OrderBy(t => UnityEngine.Random.value);
    }
}
