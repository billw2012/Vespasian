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

    public static T SelectRandom<T>(this IEnumerable<T> @this) => @this.ElementAtOrDefault(UnityEngine.Random.Range(0, @this.Count()));

    public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> @this) => @this.OrderBy(t => UnityEngine.Random.value);

    /// <summary>
    /// Wraps this object instance into an IEnumerable&lt;T&gt;
    /// consisting of a single item.
    /// </summary>
    /// <typeparam name="T"> Type of the object. </typeparam>
    /// <param name="item"> The instance that will be wrapped. </param>
    /// <returns> An IEnumerable&lt;T&gt; consisting of a single item. </returns>
    public static IEnumerable<T> Yield<T>(this T item)
    {
        yield return item;
    }
}
