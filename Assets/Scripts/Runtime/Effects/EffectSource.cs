using UnityEngine;
using System.Linq;
using UnityEditor;
using System.Collections.Generic;
using System;

public abstract class EffectSource : MonoBehaviour, ISavable
{
    [Tooltip("Max range the effect will work at")]
    public float range = 3.0f;

    //[Tooltip("Strength of the effect"), Range(0, 100)]
    //public float effectFactor = 1.0f;

    [Tooltip("Transform to use as the effect source")]
    public Transform originTransform;

    public GameObject areaMarkerAsset;

    [NonSerialized]
    [Saved]
    public bool discovered;

    float revealedTime;

    public float timeMultipler => this.simManager == null ? 1 : this.simManager.timeStep;
    
    GameObject areaMarker;

    Simulation simManager;

    void Awake()
    {
        this.simManager = FindObjectOfType<Simulation>();
        if (this.areaMarkerAsset != null)
        {
            this.areaMarker = Instantiate(this.areaMarkerAsset, this.originTransform);
            this.areaMarker.transform.localScale = Vector3.one * this.range;
            this.areaMarker.SetActive(false);
        }
    }

    void Update()
    {
        if (this.areaMarker != null)
        {
            // Show the marker only if the effect is discovered, not complete and revealed by something recently (scanner, observation etc.)
            this.areaMarker.SetActive(this.discovered && !this.IsComplete() && Time.time < this.revealedTime + 1f);
        }
    }

    public void Reveal()
    {
        this.discovered = true;
        this.revealedTime = Time.time;
    }

    // Virtual functions, must be overridden in derived effect sources

    // Must return true when this has no more resource (fully mined, fully scanned, etc)
    // It is essential to mark effect sources as complete so we don't keep focus on them and switch to next one instead
    // Proximity methods below use this method
    public virtual bool IsComplete() => false;

    public virtual float GetDistance(Transform other) => this.originTransform.worldToLocalMatrix.MultiplyPoint(other.position).xy0().magnitude;

    // Various helper functions
    // Returns true if other transform is in range of this effect source
    public bool IsInRange(Transform other) => this.GetDistance(other) < this.range;

    public bool IsInDetectionRange(Transform other, float detectionRange) => this.GetDistance(other) < this.range + detectionRange;

    // Returns value where 1 corresponds to max distance
    public float GetDistanceNormalized(Transform from) => this.GetDistance(from) / this.range;

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
            .Where(i => !i.IsComplete())
            .Select(i => (effectsrc: i, dist: i.GetDistance(tFrom)))
            .Where(i => i.dist < i.effectsrc.range)
            .OrderBy(i => i.dist)
            .FirstOrDefault().effectsrc
            ;
    }

    public static IEnumerable<T> AllInRange<T>(Transform tFrom, IEnumerable<T> sources = null) where T : EffectSource
    {
         return (sources ?? FindObjectsOfType<T>()).Where(i => i != null)
            .Where(i => !i.IsComplete() && i.IsInRange(tFrom));
    }

    public static IEnumerable<T> AllInDetectionRange<T>(Transform tFrom, float detectionRange, IEnumerable<T> sources = null) where T : EffectSource
    {
        return (sources ?? FindObjectsOfType<T>()).Where(i => i != null)
           .Where(i => !i.IsComplete() && i.IsInDetectionRange(tFrom, detectionRange));
    }

    public abstract Color gizmoColor { get; }
    public abstract string debugName { get; }

#if UNITY_EDITOR
    public virtual void OnDrawGizmosSelected()
    {
        // Display the explosion radius when selected
        Handles.color = this.gizmoColor;
        Handles.matrix = this.originTransform.localToWorldMatrix;
        Handles.DrawWireDisc(Vector3.zero, Vector3.forward, this.range);
        GUIUtils.Label(Quaternion.Euler(0, 0, this.debugName.GetHashCode() % 90) * Vector2.left * this.range * 0.95f, this.debugName);
    }

#endif
}
