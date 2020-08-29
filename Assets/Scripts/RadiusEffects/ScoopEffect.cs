using UnityEngine;

public class ScoopEffect : RadiusEffect
{
    protected override void Apply(RadiusEffectTarget target, float value, Vector3 direction)
    {
        var playerLogic = target.GetComponent<PlayerLogic>();
        if (playerLogic != null)
        {
            playerLogic.AddFuel(value);
        }
    }
};
