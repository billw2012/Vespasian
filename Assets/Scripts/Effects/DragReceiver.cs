using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DragReceiver : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    void Update()
    {
        var dragSources = EffectSource.GetEffectSourcesInRange<DragSource>(this.transform);
        if (dragSources.Length > 0)
        {
            Debug.Log("Drag is active");
            foreach (var source in dragSources)
            {
                //float fieldStrength = source.GetEffectStrengthNormalized(this.transform);

                var simMovement = GetComponent<SimMovement>();
                float velocityAbs = simMovement.velocity.magnitude;
                float value = Time.deltaTime * 3.0f * velocityAbs * velocityAbs;
                if (simMovement != null && velocityAbs > 0)
                {
                    simMovement.AddForce(-simMovement.velocity.normalized * value);
                }
            }
        }
    }
}
