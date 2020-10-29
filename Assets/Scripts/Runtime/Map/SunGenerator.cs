using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class SunGenerator : BodyGenerator
{
    public float brightness = 1f;

    StarOrPlanet star => this.body as StarOrPlanet;

    public float temp => this.star.temp * 10000;

    protected override void InitInternal(RandomX rng)
    {
        var starLogic = this.GetComponent<StarLogic>();
        var color = Mathf.CorrelatedColorTemperatureToRGB(this.temp);

        var normalizedColor = color * this.brightness / (color.r + color.g + color.b);

        starLogic.color = normalizedColor;

        // Extend radius effect range by our radius (-1 because that is the default radius that is already considered in the effect range)
        foreach (var effect in this.GetComponents<EffectSource>())
        {
            effect.range += this.star.radius - 1;
        }

        // starLogic.glowIntensity = 
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (this.body != null && this.GetComponent<GravitySource>() != null)
        {
            Handles.color = Color.blue;
            GUIUtils.Label(this.GetComponent<GravitySource>().target.position + Vector3.down * this.star.radius * 1.25f, $"{this.temp:0}°K\n{this.star.radius:0.00}R\n{this.star.mass:0.00}M");
        }
    }
#endif
}
