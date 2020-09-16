using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DamageReceiver : MonoBehaviour
{
    readonly Dictionary<RingDamageSource, Vector3> previousRelativePosMap = new Dictionary<RingDamageSource, Vector3>();

    void Start()
    {
        //this.previousPos = this.transform.position;
    }

    void Update()
    {
        var damageSources = EffectSource.AllInRange<DamageSource>(this.transform);
        var healthComponent = this.GetComponent<HealthComponent>();
        foreach (var source in damageSources)
        {
            //float fieldStrength = source.GetEffectStrengthNormalized(this.transform);
            float damagePerTime = Time.deltaTime * 0.2f;
            var direction = Vector3.Normalize(this.transform.position - source.transform.position);
            healthComponent.AddDamage(damagePerTime, direction);
        }

        var ringDamageSources = EffectSource.AllInRange<RingDamageSource>(this.transform);
        foreach (var source in ringDamageSources)
        {
            var relativePos = source.transform.worldToLocalMatrix.MultiplyPoint(this.transform.position);
            if (this.previousRelativePosMap.TryGetValue(source, out var prevRelativePos))
            {
                var relativeVelocity = (prevRelativePos - relativePos) / Time.deltaTime;
                Debug.Log($"{relativeVelocity.x} {relativeVelocity.y} {relativeVelocity.magnitude}");
                float damagePerTime = Time.deltaTime * 0.1f * relativeVelocity.sqrMagnitude * source.damageMultiplier;
                healthComponent.AddDamage(damagePerTime, source.transform.localToWorldMatrix.MultiplyVector(relativeVelocity));
            }

            this.previousRelativePosMap[source] = relativePos;
        }

        foreach(var inactiveDamageSource in this.previousRelativePosMap.Keys.Where(k => !ringDamageSources.Contains(k)).ToList())
        {
            this.previousRelativePosMap.Remove(inactiveDamageSource);
        }
    }
}
