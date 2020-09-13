using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageReceiver : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        var damageSources = EffectSource.GetEffectSourcesInRange<DamageSource>(this.transform);
        if (damageSources.Length > 0)
        {
            var healthComponent = GetComponent<HealthComponent>();
            foreach (var source in damageSources)
            {
                //float fieldStrength = source.GetEffectStrengthNormalized(this.transform);
                float damagePerTime = Time.deltaTime * 0.2f;
                Vector3 direction = Vector3.Normalize(this.transform.position - source.transform.position);
                healthComponent.AddDamage(damagePerTime, direction);
            }
        }
    }
}
