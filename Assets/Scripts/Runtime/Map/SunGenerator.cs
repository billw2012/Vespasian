using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class SunGenerator : StarOrPlanetGenerator
{
    public float brightness = 1f;

    public float temp => this.starOrPlanet.temp * 10000;

    protected override void InitInternal(RandomX rng)
    {
        base.InitInternal(rng);

        var starLogic = this.GetComponent<StarLogic>();
        var color = Mathf.CorrelatedColorTemperatureToRGB(this.temp);

        var normalizedColor = color * this.brightness / (color.r + color.g + color.b);

        starLogic.color = normalizedColor;

        // starLogic.glowIntensity = 
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (this.body != null && this.GetComponent<GravitySource>() != null)
        {
            Handles.color = Color.blue;
            GUIUtils.Label(this.GetComponent<GravitySource>().target.position + Vector3.down * this.starOrPlanet.radius * 1.25f, $"{this.temp:0}°K\n{this.starOrPlanet.radius:0.00}R\n{this.starOrPlanet.mass:0.00}M");
        }
    }
#endif
}
