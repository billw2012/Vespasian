using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Basic Star description.
/// </summary>
[RequireComponent(typeof(BodyLogic))]
public class StarLogic : MonoBehaviour
{
    public Light childLight;

    public Transform geometryTransform;
    public Transform glowTransform;

    public List<Renderer> renderers = new List<Renderer>();

    [ColorUsage(showAlpha: false, hdr: true)]
    public Color color = Color.white;

    [Tooltip("How closely the light color follows sun color"), Range(0, 1)]
    public float lightTintFactor = 0.25f;

    [Tooltip("How high the light is above the sun surface"), Range(0, 30)]
    public float lightHeight = 5f;

    [Tooltip("How intense the sun glow is"), Range(0, 1)]
    public float glowIntensity = 0.25f;

    [Tooltip("How far the sun glow spreads")]
    public float glowSpread = 1f;

    public bool isPrimary => this.geometryTransform.position == Vector3.zero;

    private void OnValidate() => this.Refresh();

    private void Start() => this.Refresh();

    private void Refresh()
    {
        float radius = this.GetComponent<BodyLogic>().radius;
        this.childLight.color = Color.Lerp(Color.white, this.color, this.lightTintFactor);
        this.childLight.transform.localPosition = Vector3.back * (this.lightHeight + radius);

        this.glowTransform.localScale = Vector3.forward + (Vector3)(Vector2.one * 40f * radius * this.glowSpread);
        this.glowTransform.localPosition = new Vector3(0, 0, 10f + radius);

        foreach (var r in this.renderers)
        {
            var pb = new MaterialPropertyBlock();
            TweakColor(pb, r.sharedMaterial, new[] {
                "_BaseColor",
                "_SpecColor",
                "_EmissionColor",
            }, this.color);
            r.SetPropertyBlock(pb);
        }
    }

    private static Color TweakH(Color color, float newH, float newAlpha = 1)
    {
        Color.RGBToHSV(color, out _, out float s, out float v);
        var result = Color.HSVToRGB(newH, s, v);
        result.a = newAlpha;
        return result;
    }

    private static void TweakH(MaterialPropertyBlock matPB, Material baseMaterial, IEnumerable<string> colorProps, float newH, float newAlpha = 1)
    {
        foreach (string colorProp in colorProps.Where(c => baseMaterial.HasProperty(c)))
        {
            matPB.SetColor(colorProp, TweakH(baseMaterial.GetColor(colorProp), newH, newAlpha));
        }
    }

    private static void TweakColor(MaterialPropertyBlock matPB, Material baseMaterial, IEnumerable<string> colorProps, Color newColor)
    {
        foreach (string colorProp in colorProps.Where(c => baseMaterial.HasProperty(c)))
        {
            matPB.SetColor(colorProp, newColor);
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
}
