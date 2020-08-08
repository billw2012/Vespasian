using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SunLogic : MonoBehaviour
{
    private Light childLight;
    //private Material mainMaterial;
    private MaterialPropertyBlock mainPB;
    //private Material glowMaterial;
    private MaterialPropertyBlock glowPB;
    //private Material pfxMaterial;
    private MaterialPropertyBlock pfxPB;

    private static Color TweakH(Color color, float newH)
    {
        Color.RGBToHSV(color, out _, out float s, out float v);
        return Color.HSVToRGB(newH, s, v);
    }

    private static void TweakH(Light light, float newH)
    {
        light.color = TweakH(light.color, newH);
    }

    private static void TweakH(Material mat, string[] colorProps, float newH)
    {
        foreach(var colorProp in colorProps)
        {
            mat.SetColor(colorProp, TweakH(mat.GetColor(colorProp), newH));
        }
    }

    private static void TweakH(MaterialPropertyBlock matPB, Material baseMaterial, string[] colorProps, float newH)
    {
        foreach (var colorProp in colorProps)
        {
            matPB.SetColor(colorProp, TweakH(baseMaterial.GetColor(colorProp), newH));
        }
    }

    // We could do this, but it doesn't produce as interesting results
    //private Color TempToColor(float kelvin)
    //{
    //    var temp = kelvin / 100;
    //    float red, green, blue;
    //    if (temp <= 66)
    //    {
    //        red = 255;
    //        green = temp;
    //        green = 99.4708025861f * Mathf.Log(green) - 161.1195681661f;
    //        if (temp <= 19)
    //        {
    //            blue = 0;
    //        }
    //        else
    //        {
    //            blue = temp - 10;
    //            blue = 138.5177312231f * Mathf.Log(blue) - 305.0447927307f;
    //        }
    //    }
    //    else
    //    {
    //        red = temp - 60;
    //        red = 329.698727446f * Mathf.Pow(red, -0.1332047592f);
    //        green = temp - 60;
    //        green = 288.1221695283f * Mathf.Pow(green, -0.0755148492f);
    //        blue = 255;
    //    }
    //    return new Color(
    //        Mathf.Clamp01(red / 255),
    //        Mathf.Clamp01(green / 255),
    //        Mathf.Clamp01(blue / 255)
    //    );
    //}
    //[Tooltip("Sun Temperature in Kelvin"), Range(100f, 40000f)]
    //public float temp = 1000;

    public Color color = Color.white;
    [Tooltip("How closely the light color follows sun color"), Range(0, 1)]
    public float lightTintFactor = 0.25f;

    void OnValidate()
    {
        this.childLight = this.transform.Find("Light").GetComponent<Light>();
        var mainRenderer = this.GetComponent<MeshRenderer>();
        var glowRenderer = this.transform.Find("Glow").GetComponent<MeshRenderer>();
        var pfxRenderer = this.transform.Find("Particles").GetComponent<ParticleSystemRenderer>();

        this.childLight.color = Color.Lerp(Color.white, color, this.lightTintFactor);

        Color.RGBToHSV(this.color, out float newH, out _, out _);

        if (this.mainPB == null)
        {
            this.mainPB = new MaterialPropertyBlock();
            this.glowPB = new MaterialPropertyBlock();
            this.pfxPB = new MaterialPropertyBlock();
        }

        TweakH(this.mainPB, mainRenderer.sharedMaterial, new[] {
            "_BaseColor",
            "_SpecColor",
            "_EmissionColor",
        }, newH);
        TweakH(this.glowPB, glowRenderer.sharedMaterial, new[] {
            "_BaseColor",
            "_SpecColor",
            "_EmissionColor",
        }, newH);
        TweakH(this.pfxPB, pfxRenderer.sharedMaterial, new[] {
            "_BaseColor",
            "_SpecColor",
            "_EmissionColor",
        }, newH);

        mainRenderer.SetPropertyBlock(this.mainPB);
        glowRenderer.SetPropertyBlock(this.glowPB);
        pfxRenderer.SetPropertyBlock(this.pfxPB);
    }
}
