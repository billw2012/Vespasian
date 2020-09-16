using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RingDamageSource : RingEffectSource
{
    public float damageMultiplier = 1f;

    public override Color gizmoColor => Color.red;
    public override string debugName => "Damage";
}
