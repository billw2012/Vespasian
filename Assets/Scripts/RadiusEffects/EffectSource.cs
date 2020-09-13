using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class EffectSource : MonoBehaviour
{
    [Tooltip("Max distance from center for effect to work"), Range(0, 10)]
    public float maxRadius = 3.0f;

    //[Tooltip("Strength of the effect"), Range(0, 100)]
    //public float effectFactor = 1.0f;

    [Tooltip("Transform to use as the effect source")]
    public Transform effectSourceTransform;


    void OnDrawGizmosSelected()
    {
        // Display the explosion radius when selected
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, this.maxRadius);
    }



    // Various helper functions

    // Returns true if other transform is in range of this effect source
    public bool IsInEffectRange(Transform other)
    {
        return Vector3.Distance(this.effectSourceTransform.position, other.position) < this.maxRadius;
    }

    // Returns value where 1 corresponds to max distance
    public float GetDistanceNormalized(Transform tFrom)
    {
        float dist = Vector3.Distance(tFrom.position, this.effectSourceTransform.position);
        return dist / this.maxRadius;
    }

    // Returns 0 if tFrom is beyond range
    // Returns 1 if tFrom is at closest range
    // Interpolates linearly in between
    public float GetEffectStrengthNormalized(Transform tFrom)
    {
        float dist = this.GetDistanceNormalized(tFrom);
        float distClamp = Mathf.Clamp(dist, 0, 1);
        return 1.0f - distClamp;
    }


    // Various static helper functions
    
    // Returns closet effect source in range, or null
    public static T GetNearestEffectSource<T>(Transform tFrom, T[] sources) where T : EffectSource
    {
        var sourcesSorted = sources.Where(i => i != null)
                                    .Select(i => new { effectsrc = i, dist = Vector3.Distance(tFrom.position, i.effectSourceTransform.position) })
                                    .Where(i => i.dist < i.effectsrc.maxRadius)
                                    .OrderBy(i => i.dist);
        return sourcesSorted.Count() > 0 ? sourcesSorted.ElementAt(0).effectsrc : null;
    }

    // Returns closet effect source in range, or null
    public static T GetNearestEffectSource<T>(Transform tFrom) where T : EffectSource
    {
        var sources = Object.FindObjectsOfType<T>();
        return EffectSource.GetNearestEffectSource<T>(tFrom, sources);
    }


    public static T[] GetEffectSourcesInRange<T>(Transform tFrom, T[] sources) where T : EffectSource
    {
        var sourcesInRange = sources.Where(i => i != null)
                                    .Where(i => Vector3.Distance(tFrom.position, i.effectSourceTransform.position) < i.maxRadius);
        return sourcesInRange.ToArray();
    }

    public static T[] GetEffectSourcesInRange<T>(Transform tFrom) where T : EffectSource
    {
        var sources = Object.FindObjectsOfType<T>();
        return EffectSource.GetEffectSourcesInRange<T>(tFrom, sources);
    }
}
