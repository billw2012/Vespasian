using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[Serializable]
public class PostEffect
{
    public PostProcessUrp target;

    public enum FloatPropertyTarget
    {
        BlurAmount,
        BloomAmount,
        BloomDiffuse,
        BloomThreshold,
        BloomSoftness,
        LutAmount,
        Contrast,
        Brightness,
        Saturation,
        Exposure,
        Gamma,
        Sharpness,
        Offset,
        FishEyeDistortion,
        GlitchAmount,
        LensDistortion,
        VignetteAmount,
        VignetteSoftness
    }
    [Serializable]
    public class FloatProperty
    {
        public FloatPropertyTarget target;
        public float targetValue;
        [NonSerialized]
        public float originalValue;
        [NonSerialized]
        public FieldInfo field;
        public AnimationCurve curve;
    }
    public List<FloatProperty> floatProperties;

    public enum ColorPropertyTarget
    {
        BloomColor,
        Color,
        VignetteColor
    }

    [Serializable]
    public class ColorProperty
    {
        public ColorPropertyTarget target;
        public Color targetValue;
        [NonSerialized]
        public Color originalValue;
        [NonSerialized]
        public FieldInfo field;
        public AnimationCurve curve;
    }
    public List<ColorProperty> colorProperties;

    public void Init()
    {
        AnimationCurve NormalizeAnimation(AnimationCurve curve)
        {
            if (curve.keys.Length == 0)
            {
                return AnimationCurve.Linear(0, 0, 1, 1);
            }
            else
            {
                float maxTime = curve.keys.Last().time;
                for (int i = 0; i < curve.keys.Length; i++)
                {
                    curve.keys[i].time = curve.keys[i].time / maxTime;
                }
                return curve;
            }
        }

        foreach (var p in this.colorProperties)
        {
            p.field = typeof(PostProcessUrp.PostProcessSettings).GetField(p.target.ToString());
            p.originalValue = (Color)p.field.GetValue(this.target.runtimeSettings);
            p.curve = NormalizeAnimation(p.curve);
        }
        foreach (var p in this.floatProperties)
        {
            p.field = typeof(PostProcessUrp.PostProcessSettings).GetField(p.target.ToString());
            p.originalValue = (float)p.field.GetValue(this.target.runtimeSettings);
            p.curve = NormalizeAnimation(p.curve);
        }
    }

    public void Update(float t)
    {
        foreach (var p in this.colorProperties)
        {
            var v = Color.Lerp(p.originalValue, p.targetValue, p.curve.Evaluate(t));
            p.field.SetValue(this.target.runtimeSettings, v);
        }
        foreach (var p in this.floatProperties)
        {
            float v = Mathf.Lerp(p.originalValue, p.targetValue, p.curve.Evaluate(t));
            p.field.SetValue(this.target.runtimeSettings, v);
        }
    }

    public void ResetSettings()
    {
        foreach (var p in this.colorProperties)
        {
            p.field?.SetValue(this.target.runtimeSettings, p.originalValue);
        }
        foreach (var p in this.floatProperties)
        {
            p.field?.SetValue(this.target.runtimeSettings, p.originalValue);
        }
    }
}
