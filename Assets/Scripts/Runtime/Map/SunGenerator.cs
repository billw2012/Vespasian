using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SunGenerator : BodyGenerator
{
    public float brightness = 1f;

    public override float tempK => this.body.temp * 10000;

    protected override void InitInternal(Body body, float danger)
    {
        var starLogic = this.GetComponent<StarLogic>();
        var color = Mathf.CorrelatedColorTemperatureToRGB(body.temp * 10000f);

        var normalizedColor = color * this.brightness / (color.r + color.g + color.b);

        starLogic.color = normalizedColor;
        // starLogic.glowIntensity = 
    }
}
