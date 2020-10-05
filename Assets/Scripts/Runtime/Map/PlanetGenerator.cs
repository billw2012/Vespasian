using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlanetGenerator : BodyGenerator
{
    [Tooltip("Chance this planet has a ring"), Range(0, 1)]
    public float ringChance = 0.1f;

    public WeightedRandom ringInnerRadiusRandom = new WeightedRandom { min = 0.5f, max = 1f, gaussian = true };
    public WeightedRandom ringWidthRandom = new WeightedRandom { min = 0, max = 1.5f };
    public WeightedRandom ringSaturationRandom = new WeightedRandom { min = 0, max = 1f };
    public WeightedRandom ringContrastRandom = new WeightedRandom { min = 0, max = 1f };

    public WeightedRandom hueRandom = new WeightedRandom { min = 0, max = 1f };

    protected override void InitInternal(Body body, float danger)
    {
        // Body characteristics
        var bodyLogic = this.GetComponent<BodyLogic>();
        bodyLogic.geometry.localRotation = Quaternion.Euler(MathX.RandomGaussian(-90f, 90f), 0, 0);

        // Ring
        var ring = this.GetComponentInChildren<PlanetRingRenderer>();
        if(ring != null && Random.value <= this.ringChance)
        {
            ring.enabled = true;
            ring.innerRadius = this.ringInnerRadiusRandom.Evaluate(Random.value);
            ring.width = this.ringWidthRandom.Evaluate(Random.value);
            ring.color = Random.ColorHSV();
            ring.emissive = Random.ColorHSV();
            ring.saturation = this.ringSaturationRandom.Evaluate(Random.value);
            ring.contrast = this.ringContrastRandom.Evaluate(Random.value);
            ring.patternSelect = Random.value;
            ring.patternOffset = Random.value;
        }
        else if (ring != null)
        {
            ring.enabled = false;
        }

        // Color
        var material = bodyLogic.geometry.GetComponent<Renderer>().material;
        material.SetFloat("_AlbedoHue", this.hueRandom.Evaluate(Random.value));
    }
}
