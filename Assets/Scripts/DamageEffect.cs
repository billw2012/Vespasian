using UnityEngine;

public class DamageEffect : RadiusEffect
{
    protected override void Apply(float value, Vector3 direction)
    {
        GameLogic.Instance.AddDamage(value * 0.1f, direction);
    }
};
