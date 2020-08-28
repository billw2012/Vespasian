using UnityEngine;

public class DragEffect : RadiusEffect
{
    protected override void Apply(RadiusEffectTarget target, float value, Vector3 direction)
    {
        var playerLogic = target.GetComponent<PlayerLogic>();
        if (playerLogic != null)
        {
            playerLogic.velocity = Vector3.ClampMagnitude(playerLogic.velocity, playerLogic.velocity.magnitude - value);
        }
    }
};
