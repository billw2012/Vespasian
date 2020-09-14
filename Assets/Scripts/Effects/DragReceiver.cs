using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DragReceiver : MonoBehaviour
{
    void Update()
    {
        var dragSources = EffectSource.GetEffectSourcesInRange<DragSource>(this.transform);
        foreach (var source in dragSources)
        {
            float fieldStrength = source.GetEffectStrengthNormalized(this.transform);

            var simMovement = this.GetComponent<SimMovement>();
            float velocityAbs = simMovement.velocity.magnitude;
            float value = Time.deltaTime * 3.0f * velocityAbs * velocityAbs * fieldStrength;
            if (simMovement != null && velocityAbs > 0)
            {
                simMovement.AddForce(-simMovement.velocity.normalized * value);
            }
        }
    }
}
