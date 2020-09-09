using System;
using UnityEngine;

public abstract class RadiusEffect : MonoBehaviour
{
    [Tooltip("Height at which effect starts above surface"), Range(0, 10)]
    public float maxRadius = 3.0f;

    [Tooltip("Strength of the effect"), Range(0, 100)]
    public float effectFactor = 1.0f;

    [Tooltip("Object to use as the effect source")]
    public Transform effector;

    void OnValidate()
    {
        if (this.effector == null)
        {
            var orbit = this.GetComponentInParent<Orbit>();
            this.effector = orbit != null && (orbit.gameObject == this || orbit.gameObject == this.transform.parent.gameObject)
                ? orbit.position
                : this.transform;
        }
    }

    void Start()
    {
        this.OnValidate();
    }

    void Update()
    {
        float radius = this.effector.transform.localScale.x;

        foreach(var target in FindObjectsOfType<RadiusEffectTarget>())
        {
            var targetVec = target.transform.position - this.effector.transform.position;

            float targetHeight = targetVec.magnitude;
            float heightRatio = 1 - (targetHeight - radius) / this.maxRadius;

            float effect = Mathf.Max(0, Time.deltaTime * this.effectFactor * heightRatio);
            this.Apply(target, effect, heightRatio, targetVec.normalized);
        }
    }

    protected virtual void Apply(RadiusEffectTarget target, float value, float heightRatio, Vector3 direction) { }
}
