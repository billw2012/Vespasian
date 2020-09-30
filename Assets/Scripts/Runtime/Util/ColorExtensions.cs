using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public static class ColorExtensions
{
    public static float Hue(this Color color)
    {
        Color.RGBToHSV(color, out float h, out _, out _);
        return h;
    }

    public static float Saturation(this Color color)
    {
        Color.RGBToHSV(color, out _, out float s, out _);
        return s;
    }

    public static float Value(this Color color)
    {
        Color.RGBToHSV(color, out _, out _, out float v);
        return v;
    }

    public static Color Saturate(this Color color, float saturation)
    {
        Color.RGBToHSV(color, out float h, out float s, out float v);
        return Color.HSVToRGB(h, Mathf.Clamp01(s + saturation), v);
    }

    public static Color ShiftHue(this Color color, float shift)
    {
        Color.RGBToHSV(color, out float h, out float s, out float v);
        return Color.HSVToRGB((h + shift) % 1f, s, v);
    }

    public static Color Brighten(this Color color, float brighten)
    {
        Color.RGBToHSV(color, out float h, out float s, out float v);
        return Color.HSVToRGB(h, s, Mathf.Clamp01(v + brighten));
    }

    public static Color SetR(this Color color, float r) => new Color(r, color.g, color.b, color.a);
    public static Color SetG(this Color color, float g) => new Color(color.r, g, color.b, color.a);
    public static Color SetB(this Color color, float b) => new Color(color.r, color.g, b, color.a);
    public static Color SetA(this Color color, float a) => new Color(color.r, color.g, color.b, a);
}
