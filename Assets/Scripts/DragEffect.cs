using UnityEngine;

public class DragEffect : RadiusEffect
{
    protected override void Apply(float value, Vector3 direction)
    {
        var playerLogic = GameLogic.Instance.player.GetComponent<PlayerLogic>();
        playerLogic.velocity = Vector3.ClampMagnitude(playerLogic.velocity, playerLogic.velocity.magnitude - value);
    }
};
