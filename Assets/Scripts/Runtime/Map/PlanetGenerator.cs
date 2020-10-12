using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class PlanetGenerator : BodyGenerator
{
    [Tooltip("Chance this planet has a ring"), Range(0, 1)]
    public float ringChance = 0.66f;
    public float ringMassMin = 4.5f;

    public MeshRenderer planetRenderer;

    public WeightedRandom ringInnerRadiusRandom = new WeightedRandom { min = 0.5f, max = 1f, gaussian = true };
    public WeightedRandom ringWidthRandom = new WeightedRandom { min = 0, max = 1.5f };
    public WeightedRandom ringSaturationRandom = new WeightedRandom { min = 0, max = 1f };
    public WeightedRandom ringContrastRandom = new WeightedRandom { min = 0, max = 1f };

    public WeightedRandom hueRandom = new WeightedRandom { min = 0, max = 1f };

    StarOrPlanet planet => this.body as StarOrPlanet;

    protected override void InitInternal()
    {
        // Body characteristics
        var bodyLogic = this.GetComponent<BodyLogic>();
        bodyLogic.geometry.localRotation = Quaternion.Euler(MathX.RandomGaussian(-90f, 90f), 0, 0);

        // Ring
        var ring = this.GetComponentInChildren<PlanetRingRenderer>();
        if(ring != null && planet.mass >= this.ringMassMin && Random.value <= this.ringChance)
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
        var material = this.planetRenderer.material;
        material.SetFloat("_AlbedoHue", this.hueRandom.Evaluate(Random.value));
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (this.body != null && this.GetComponent<GravitySource>() != null)
        {
            Handles.color = Color.yellow;
            GUIUtils.Label(this.GetComponent<GravitySource>().target.position + Vector3.down * this.planet.radius * 1.25f, $"{this.planet.temp:0}°K\n{this.planet.radius:0.00}R\n{this.planet.mass:0.00}M");
        }
    }
#endif
}
