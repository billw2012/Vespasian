using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SunLogic : MonoBehaviour
{
    public Light childLight;
    public MeshRenderer mainRenderer;
    public MeshRenderer glowRenderer;
    public ParticleSystemRenderer pfxRenderer;

    private MaterialPropertyBlock mainPB;
    private MaterialPropertyBlock glowPB;
    private MaterialPropertyBlock pfxPB;

    private static Color TweakH(Color color, float newH)
    {
        Color.RGBToHSV(color, out _, out float s, out float v);
        return Color.HSVToRGB(newH, s, v);
    }

    private static void TweakH(MaterialPropertyBlock matPB, Material baseMaterial, string[] colorProps, float newH)
    {
        foreach (var colorProp in colorProps.Where(c => baseMaterial.HasProperty(c)))
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
    [Tooltip("How high the light is above the sun surface"), Range(0, 30)]
    public float lightHeight = 5f;

    void UpdateDependentColors()
    {
        this.childLight.color = Color.Lerp(Color.white, color, this.lightTintFactor);
        this.childLight.transform.localPosition = Vector3.back * (this.lightHeight + this.mainRenderer.transform.localScale.z);

        Color.RGBToHSV(this.color, out float newH, out _, out _);

        if (this.mainPB == null)
        {
            this.mainPB = new MaterialPropertyBlock();
            this.glowPB = new MaterialPropertyBlock();
            this.pfxPB = new MaterialPropertyBlock();
        }

        TweakH(this.mainPB, this.mainRenderer.sharedMaterial, new[] {
            "_BaseColor",
            "_SpecColor",
            "_EmissionColor",
        }, newH);
        TweakH(this.glowPB, this.glowRenderer.sharedMaterial, new[] {
            "_BaseColor",
            "_SpecColor",
            "_EmissionColor",
        }, newH);
        TweakH(this.pfxPB, this.pfxRenderer.sharedMaterial, new[] {
            "_BaseColor",
            "_SpecColor",
            "_EmissionColor",
        }, newH);

        this.mainRenderer.SetPropertyBlock(this.mainPB);
        this.glowRenderer.SetPropertyBlock(this.glowPB);
        this.pfxRenderer.SetPropertyBlock(this.pfxPB);
    }

    void OnValidate()
    {
        this.UpdateDependentColors();
    }

    void Start()
    {
        this.UpdateDependentColors();
    }
}
