using UnityEngine;

public class DamageEffect : RadiusEffect
{
    protected override void Apply(RadiusEffectTarget target, float value, float heightRatio, Vector3 direction)
    {
        var healthComponent = target.GetComponent<HealthComponent>();
        if (healthComponent != null)
        {
            healthComponent.AddDamage(value * 0.1f, direction);
        }
    }
};
