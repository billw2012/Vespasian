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

    private static Color TweakHS(Color color, float newH, float newS)
    {
        Color.RGBToHSV(color, out _, out _, out float v);
        return Color.HSVToRGB(newH, newS, v);
    }

    private static void TweakHS(Light light, float newH, float newS)
    {
        light.color = TweakHS(light.color, newH, newS);
    }

    private static void TweakHS(Material mat, string[] colorProps, float newH, float newS)
    {
        foreach(var colorProp in colorProps)
        {
            mat.SetColor(colorProp, TweakHS(mat.GetColor(colorProp), newH, newS));
        }
    }

    private static void TweakHS(MaterialPropertyBlock matPB, Material baseMaterial, string[] colorProps, float newH, float newS)
    {
        foreach (var colorProp in colorProps)
        {
            matPB.SetColor(colorProp, TweakHS(baseMaterial.GetColor(colorProp), newH, newS));
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

    void OnValidate()
    {
        this.childLight = this.transform.Find("Light").GetComponent<Light>();
        var mainRenderer = this.GetComponent<MeshRenderer>();
        var glowRenderer = this.transform.Find("Glow").GetComponent<MeshRenderer>();
        var pfxRenderer = this.transform.Find("Particles").GetComponent<ParticleSystemRenderer>();

        //var hue = TempToColor(this.temp);

        Color.RGBToHSV(this.color, out float newH, out float newS, out _);
        TweakHS(this.childLight, newH, newS);
        //this.childLight.color = color;

        if (this.mainPB == null)
        {
            this.mainPB = new MaterialPropertyBlock();
            this.glowPB = new MaterialPropertyBlock();
            this.pfxPB = new MaterialPropertyBlock();
        }

        TweakHS(this.mainPB, mainRenderer.sharedMaterial, new[] {
            "_BaseColor",
            "_SpecColor",
            "_EmissionColor",
        }, newH, newS);
        TweakHS(this.glowPB, glowRenderer.sharedMaterial, new[] {
            "_BaseColor",
            "_SpecColor",
            "_EmissionColor",
        }, newH, newS);
        TweakHS(this.pfxPB, pfxRenderer.sharedMaterial, new[] {
            "_BaseColor",
            "_SpecColor",
            "_EmissionColor",
        }, newH, newS);

        mainRenderer.SetPropertyBlock(this.mainPB);
        glowRenderer.SetPropertyBlock(this.glowPB);
        pfxRenderer.SetPropertyBlock(this.pfxPB);
    }
}
