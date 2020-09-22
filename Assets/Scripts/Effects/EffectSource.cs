using UnityEngine;
using System.Linq;
using UnityEditor;
using System.Collections.Generic;

public abstract class EffectSource : MonoBehaviour
{
    [Tooltip("Max range the effect will work at"), Range(0, 10)]
    public float range = 3.0f;

    //[Tooltip("Strength of the effect"), Range(0, 100)]
    //public float effectFactor = 1.0f;

    [Tooltip("Transform to use as the effect source")]
    public Transform originTransform;

    // Virtual functions, must be overidden in derived effect sources

    // Must return true when this has no more resource (fully mined, fully scanned, etc)
    // It is essential to mark effect sources as complete so we don't keep focus on them and switch to next one instead
    // Proximity methods below use this method
    public virtual bool IsEmpty() => false;

    public virtual float GetDistance(Transform other)
    {
        return this.originTransform.worldToLocalMatrix.MultiplyPoint(other.position).xy0().magnitude;
    }

    // Various helper functions
    // Returns true if other transform is in range of this effect source
    public bool IsInRange(Transform other)
    {
        return this.GetDistance(other) < this.range;
    }

    // Returns value where 1 corresponds to max distance
    public float GetDistanceNormalized(Transform from)
    {
        return this.GetDistance(from) / this.range;
    }

    // Returns 0 if tFrom is beyond range
    // Returns 1 if tFrom is at closest range
    // Interpolates linearly in between
    public float GetEffectStrengthNormalized(Transform tFrom)
    {
        float dist = this.GetDistanceNormalized(tFrom);
        float distClamp = Mathf.Clamp01(dist);
        return 1.0f - distClamp;
    }

    // Various static helper functions

    // Returns closet effect source in range, or null
    public static T GetNearest<T>(Transform tFrom, IEnumerable<T> sources = null) where T : EffectSource
    {
        return (sources ?? FindObjectsOfType<T>()).Where(i => i != null)
            .Where(i => !i.IsEmpty())
            .Select(i => (effectsrc: i, dist: i.GetDistance(tFrom)))
            .Where(i => i.dist < i.effectsrc.range)
            .OrderBy(i => i.dist)
            .FirstOrDefault().effectsrc
            ;
    }

    public static IEnumerable<T> AllInRange<T>(Transform tFrom, IEnumerable<T> sources = null) where T : EffectSource
    {
         return (sources ?? FindObjectsOfType<T>()).Where(i => i != null)
            .Where(i => !i.IsEmpty() && i.IsInRange(tFrom));
    }

    public abstract Color gizmoColor { get; }
    public abstract string debugName { get; }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // Display the explosion radius when selected
        Handles.color = this.gizmoColor;
        Handles.matrix = this.originTransform.localToWorldMatrix;
        Handles.DrawWireDisc(Vector3.zero, Vector3.forward, this.range);
        GUIUtils.Label(Quaternion.Euler(0, 0, this.debugName.GetHashCode() % 90) * Vector2.left * this.range * 0.95f, this.debugName);
    }
#endif
}
