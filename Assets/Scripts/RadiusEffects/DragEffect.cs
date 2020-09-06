using UnityEngine;

public class DragEffect : RadiusEffect
{
    protected override void Apply(RadiusEffectTarget target, float value, float heightRatio, Vector3 direction)
    {
        var simMovement = target.GetComponent<SimMovement>();
        if (simMovement != null && simMovement.velocity.magnitude > 0)
        {
            simMovement.AddForce(-simMovement.velocity.normalized * value);
        }
    }
};
