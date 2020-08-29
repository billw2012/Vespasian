using UnityEngine;

public abstract class RadiusEffect : MonoBehaviour
{
    [Tooltip("Radius at which effect starts, as a multiplier of the radius of the effector (determined from localScale)"), Range(0, 10)]
    public float maxRadiusMultiplier = 1.0f;

    [Tooltip("Strength of the effect"), Range(0, 100)]
    public float effectFactor = 1.0f;

    [Tooltip("Object to use as the effect source")]
    public Transform effector;

    void Update()
    {
        float radius = this.effector.transform.localScale.x;

        foreach(var target in FindObjectsOfType<RadiusEffectTarget>())
        {
            var targetVec = target.transform.position - this.effector.transform.position;

            float targetHeight = targetVec.magnitude;
            float heightRatio = 1 - (targetHeight - radius) / (radius * this.maxRadiusMultiplier);
            if(heightRatio > 0)
            {
                float effect = Mathf.Max(0, Time.deltaTime * this.effectFactor * heightRatio);
                this.Apply(target, effect, targetVec.normalized);
            }
        }
    }

    protected virtual void Apply(RadiusEffectTarget target, float value, Vector3 direction) { }
}
