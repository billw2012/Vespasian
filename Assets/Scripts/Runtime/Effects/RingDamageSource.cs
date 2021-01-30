using UnityEngine;

public class RingDamageSource : RingEffectSource
{
    public float damageMultiplier = 1f;

    public override Color gizmoColor => Color.red;
    public override string debugName => "Damage";
}
