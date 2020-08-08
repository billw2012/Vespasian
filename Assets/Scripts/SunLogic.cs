using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SunLogic : MonoBehaviour
{
    private Light childLight;
    private Material mainMaterial;
    private Material glowMaterial;
    private Material pfxMaterial;

    private static Color TweakHue(Color color, float newHue)
    {
        Color.RGBToHSV(color, out _, out float s, out float v);
        return Color.HSVToRGB(newHue, s, v);
    }

    private static void TweakHue(Light light, float newHue)
    {
        light.color = TweakHue(light.color, newHue);
    }

    private static void TweakHue(Material mat, float newHue)
    {
        mat.color = TweakHue(mat.color, newHue);
        var colorProps = new[] {
            "_BaseColor",
            "_SpecColor",
            "_EmissionColor",
        };
        foreach(var colorProp in colorProps)
        {
            mat.SetColor(colorProp, TweakHue(mat.GetColor(colorProp), newHue));
        }
    }

    //public Color hue
    //{
    //    get {
    //        Color.RGBToHSV(this.childLight.color, out float h, out _, out _);
    //        return TweakHue(Color.white, h);
    //    }

    //    set {
    //        Color.RGBToHSV(value, out float newHue, out _, out _);
    //        TweakHue(this.childLight, newHue);
    //        TweakHue(this.glowMaterial, newHue);
    //        TweakHue(this.mainMaterial, newHue);
    //        TweakHue(this.pfxMaterial, newHue);
    //    }
    //}

    public Color hue;

    void OnValidate()
    {
        
    }

    // Start is called before the first frame update
    void Start()
    {
        this.childLight = this.transform.Find("Light").GetComponent<Light>();
        this.mainMaterial = this.GetComponent<MeshRenderer>().material;
        this.glowMaterial = this.transform.Find("Glow").GetComponent<MeshRenderer>().material;
        this.pfxMaterial= this.transform.Find("Particles").GetComponent<ParticleSystemRenderer>().material;

        Color.RGBToHSV(this.hue, out float newHue, out _, out _);
        TweakHue(this.childLight, newHue);
        TweakHue(this.glowMaterial, newHue);
        TweakHue(this.mainMaterial, newHue);
        TweakHue(this.pfxMaterial, newHue);
    }
}
