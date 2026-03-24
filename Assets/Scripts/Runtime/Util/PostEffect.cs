using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[Serializable]
public class PostEffect
{
    public Volume volume;
    // Assign a blur material here if you need to animate blur amount via a Full Screen Pass Renderer Feature
    public Material blurMaterial;

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
        Gamma,      // no URP built-in equivalent, no-op
        Sharpness,  // no URP built-in equivalent, no-op
        ChromaticAberrationIntensity,
        FishEyeDistortion,
        GlitchAmount, // no URP built-in equivalent, no-op
        LensDistortion,
        VignetteAmount,
        VignetteSoftness,
        HueShift,
        FilmGrainIntensity,
        FilmGrainResponse,
        SplitToningBalance
    }

    [Serializable]
    public class FloatProperty
    {
        public FloatPropertyTarget target;
        public float targetValue;
        [NonSerialized] public float originalValue;
        public AnimationCurve curve;
    }
    public List<FloatProperty> floatProperties;

    public enum ColorPropertyTarget
    {
        BloomColor,
        Color,
        VignetteColor,
        SplitToningShadows,
        SplitToningHighlights,
        LiftColor,
        GammaColor,
        GainColor,
        ShadowsMidtonesHighlightsShadows,
        ShadowsMidtonesHighlightsMidtones,
        ShadowsMidtonesHighlightsHighlights
    }

    [Serializable]
    public class ColorProperty
    {
        public ColorPropertyTarget target;
        public Color targetValue;
        [NonSerialized] public Color originalValue;
        public AnimationCurve curve;
    }
    public List<ColorProperty> colorProperties;

    public void Init()
    {
        if (volume == null)
            volume = ComponentCache.FindObjectOfType<Volume>();

        AnimationCurve NormalizeAnimation(AnimationCurve curve)
        {
            if (curve.keys.Length == 0)
                return AnimationCurve.Linear(0, 0, 1, 1);
            float maxTime = curve.keys.Last().time;
            for (int i = 0; i < curve.keys.Length; i++)
                curve.keys[i].time = curve.keys[i].time / maxTime;
            return curve;
        }

        foreach (var p in this.colorProperties)
        {
            p.originalValue = GetColor(p.target);
            p.curve = NormalizeAnimation(p.curve);
        }
        foreach (var p in this.floatProperties)
        {
            p.originalValue = GetFloat(p.target);
            p.curve = NormalizeAnimation(p.curve);
        }
    }

    public void Update(float t)
    {
        foreach (var p in this.colorProperties)
            SetColor(p.target, Color.Lerp(p.originalValue, p.targetValue, p.curve.Evaluate(t)));
        foreach (var p in this.floatProperties)
            SetFloat(p.target, Mathf.Lerp(p.originalValue, p.targetValue, p.curve.Evaluate(t)));
    }

    public void ResetSettings()
    {
        foreach (var p in this.colorProperties)
            SetColor(p.target, p.originalValue);
        foreach (var p in this.floatProperties)
            SetFloat(p.target, p.originalValue);
    }

    // Values are stored in plugin-compatible units.
    // Contrast/Saturation: plugin uses -1..1, URP uses -100..100 — conversion applied on read/write.
    private float GetFloat(FloatPropertyTarget t) => GetFloat(volume?.profile, t, blurMaterial);
    private void SetFloat(FloatPropertyTarget t, float value) => SetFloat(volume?.profile, t, value, blurMaterial);

    public static float GetFloat(VolumeProfile profile, FloatPropertyTarget t, Material blurMaterial = null)
    {
        switch (t)
        {
            case FloatPropertyTarget.BlurAmount:
                return blurMaterial != null ? blurMaterial.GetFloat("_BlurAmount") : 0f;
            case FloatPropertyTarget.BloomAmount:
                return profile != null && profile.TryGet<Bloom>(out var bloom) ? bloom.intensity.value : 0f;
            case FloatPropertyTarget.BloomDiffuse:
            case FloatPropertyTarget.BloomSoftness:
                return profile != null && profile.TryGet<Bloom>(out var bloomS) ? bloomS.scatter.value : 0f;
            case FloatPropertyTarget.BloomThreshold:
                return profile != null && profile.TryGet<Bloom>(out var bloomT) ? bloomT.threshold.value : 0f;
            case FloatPropertyTarget.LutAmount:
                return profile != null && profile.TryGet<ColorLookup>(out var lut) ? lut.contribution.value : 0f;
            case FloatPropertyTarget.Contrast:
                return profile != null && profile.TryGet<ColorAdjustments>(out var caC) ? caC.contrast.value / 100f : 0f;
            case FloatPropertyTarget.Saturation:
                return profile != null && profile.TryGet<ColorAdjustments>(out var caS) ? caS.saturation.value / 100f : 0f;
            case FloatPropertyTarget.Brightness:
            case FloatPropertyTarget.Exposure:
                return profile != null && profile.TryGet<ColorAdjustments>(out var caE) ? caE.postExposure.value : 0f;
            case FloatPropertyTarget.ChromaticAberrationIntensity:
                return profile != null && profile.TryGet<ChromaticAberration>(out var ca) ? ca.intensity.value : 0f;
            case FloatPropertyTarget.FishEyeDistortion:
            case FloatPropertyTarget.LensDistortion:
                return profile != null && profile.TryGet<LensDistortion>(out var ld) ? ld.intensity.value : 0f;
            case FloatPropertyTarget.VignetteAmount:
                return profile != null && profile.TryGet<Vignette>(out var vigA) ? vigA.intensity.value : 0f;
            case FloatPropertyTarget.VignetteSoftness:
                return profile != null && profile.TryGet<Vignette>(out var vigS) ? vigS.smoothness.value : 0f;
            case FloatPropertyTarget.HueShift:
                return profile != null && profile.TryGet<ColorAdjustments>(out var caH) ? caH.hueShift.value : 0f;
            case FloatPropertyTarget.FilmGrainIntensity:
                return profile != null && profile.TryGet<FilmGrain>(out var fgI) ? fgI.intensity.value : 0f;
            case FloatPropertyTarget.FilmGrainResponse:
                return profile != null && profile.TryGet<FilmGrain>(out var fgR) ? fgR.response.value : 0f;
            case FloatPropertyTarget.SplitToningBalance:
                return profile != null && profile.TryGet<SplitToning>(out var stB) ? stB.balance.value : 0f;
            default:
                return 0f;
        }
    }

    public static void SetFloat(VolumeProfile profile, FloatPropertyTarget t, float value, Material blurMaterial = null)
    {
        switch (t)
        {
            case FloatPropertyTarget.BlurAmount:
                if (blurMaterial != null) blurMaterial.SetFloat("_BlurAmount", value);
                break;
            case FloatPropertyTarget.BloomAmount:
                if (profile != null && profile.TryGet<Bloom>(out var bloom)) bloom.intensity.value = value;
                break;
            case FloatPropertyTarget.BloomDiffuse:
            case FloatPropertyTarget.BloomSoftness:
                if (profile != null && profile.TryGet<Bloom>(out var bloomS)) bloomS.scatter.value = value;
                break;
            case FloatPropertyTarget.BloomThreshold:
                if (profile != null && profile.TryGet<Bloom>(out var bloomT)) bloomT.threshold.value = value;
                break;
            case FloatPropertyTarget.LutAmount:
                if (profile != null && profile.TryGet<ColorLookup>(out var lut)) lut.contribution.value = value;
                break;
            case FloatPropertyTarget.Contrast:
                if (profile != null && profile.TryGet<ColorAdjustments>(out var caC)) caC.contrast.value = value * 100f;
                break;
            case FloatPropertyTarget.Saturation:
                if (profile != null && profile.TryGet<ColorAdjustments>(out var caS)) caS.saturation.value = value * 100f;
                break;
            case FloatPropertyTarget.Brightness:
            case FloatPropertyTarget.Exposure:
                if (profile != null && profile.TryGet<ColorAdjustments>(out var caE)) caE.postExposure.value = value;
                break;
            case FloatPropertyTarget.ChromaticAberrationIntensity:
                if (profile != null && profile.TryGet<ChromaticAberration>(out var ca)) ca.intensity.value = Mathf.Clamp01(value);
                break;
            case FloatPropertyTarget.FishEyeDistortion:
            case FloatPropertyTarget.LensDistortion:
                if (profile != null && profile.TryGet<LensDistortion>(out var ld)) ld.intensity.value = value;
                break;
            case FloatPropertyTarget.VignetteAmount:
                if (profile != null && profile.TryGet<Vignette>(out var vigA)) vigA.intensity.value = value;
                break;
            case FloatPropertyTarget.VignetteSoftness:
                if (profile != null && profile.TryGet<Vignette>(out var vigS)) vigS.smoothness.value = value;
                break;
            case FloatPropertyTarget.HueShift:
                if (profile != null && profile.TryGet<ColorAdjustments>(out var caH)) caH.hueShift.value = value;
                break;
            case FloatPropertyTarget.FilmGrainIntensity:
                if (profile != null && profile.TryGet<FilmGrain>(out var fgI)) fgI.intensity.value = value;
                break;
            case FloatPropertyTarget.FilmGrainResponse:
                if (profile != null && profile.TryGet<FilmGrain>(out var fgR)) fgR.response.value = value;
                break;
            case FloatPropertyTarget.SplitToningBalance:
                if (profile != null && profile.TryGet<SplitToning>(out var stB)) stB.balance.value = value;
                break;
        }
    }

    private Color GetColor(ColorPropertyTarget t)
    {
        var profile = volume?.profile;
        switch (t)
        {
            case ColorPropertyTarget.BloomColor:
                return profile != null && profile.TryGet<Bloom>(out var bloom) ? bloom.tint.value : Color.white;
            case ColorPropertyTarget.Color:
                return profile != null && profile.TryGet<ColorAdjustments>(out var ca) ? ca.colorFilter.value : Color.white;
            case ColorPropertyTarget.VignetteColor:
                return profile != null && profile.TryGet<Vignette>(out var vig) ? vig.color.value : Color.black;
            case ColorPropertyTarget.SplitToningShadows:
                return profile != null && profile.TryGet<SplitToning>(out var stSh) ? stSh.shadows.value : Color.grey;
            case ColorPropertyTarget.SplitToningHighlights:
                return profile != null && profile.TryGet<SplitToning>(out var stHi) ? stHi.highlights.value : Color.grey;
            case ColorPropertyTarget.LiftColor:
                return profile != null && profile.TryGet<LiftGammaGain>(out var lggL) ? VecToColor(lggL.lift.value) : Color.white;
            case ColorPropertyTarget.GammaColor:
                return profile != null && profile.TryGet<LiftGammaGain>(out var lggG) ? VecToColor(lggG.gamma.value) : Color.white;
            case ColorPropertyTarget.GainColor:
                return profile != null && profile.TryGet<LiftGammaGain>(out var lggGn) ? VecToColor(lggGn.gain.value) : Color.white;
            case ColorPropertyTarget.ShadowsMidtonesHighlightsShadows:
                return profile != null && profile.TryGet<ShadowsMidtonesHighlights>(out var smhSh) ? VecToColor(smhSh.shadows.value) : Color.white;
            case ColorPropertyTarget.ShadowsMidtonesHighlightsMidtones:
                return profile != null && profile.TryGet<ShadowsMidtonesHighlights>(out var smhMi) ? VecToColor(smhMi.midtones.value) : Color.white;
            case ColorPropertyTarget.ShadowsMidtonesHighlightsHighlights:
                return profile != null && profile.TryGet<ShadowsMidtonesHighlights>(out var smhHi) ? VecToColor(smhHi.highlights.value) : Color.white;
            default:
                return Color.white;
        }
    }

    private static Color VecToColor(Vector4 v) => new Color(v.x, v.y, v.z, v.w);
    private static Vector4 ColorToVec(Color c) => new Vector4(c.r, c.g, c.b, c.a);

    private void SetColor(ColorPropertyTarget t, Color value)
    {
        var profile = volume?.profile;
        switch (t)
        {
            case ColorPropertyTarget.BloomColor:
                if (profile != null && profile.TryGet<Bloom>(out var bloom)) bloom.tint.value = value;
                break;
            case ColorPropertyTarget.Color:
                if (profile != null && profile.TryGet<ColorAdjustments>(out var ca)) ca.colorFilter.value = value;
                break;
            case ColorPropertyTarget.VignetteColor:
                if (profile != null && profile.TryGet<Vignette>(out var vig)) vig.color.value = value;
                break;
            case ColorPropertyTarget.SplitToningShadows:
                if (profile != null && profile.TryGet<SplitToning>(out var stSh)) stSh.shadows.value = value;
                break;
            case ColorPropertyTarget.SplitToningHighlights:
                if (profile != null && profile.TryGet<SplitToning>(out var stHi)) stHi.highlights.value = value;
                break;
            case ColorPropertyTarget.LiftColor:
                if (profile != null && profile.TryGet<LiftGammaGain>(out var lggL)) lggL.lift.value = ColorToVec(value);
                break;
            case ColorPropertyTarget.GammaColor:
                if (profile != null && profile.TryGet<LiftGammaGain>(out var lggG)) lggG.gamma.value = ColorToVec(value);
                break;
            case ColorPropertyTarget.GainColor:
                if (profile != null && profile.TryGet<LiftGammaGain>(out var lggGn)) lggGn.gain.value = ColorToVec(value);
                break;
            case ColorPropertyTarget.ShadowsMidtonesHighlightsShadows:
                if (profile != null && profile.TryGet<ShadowsMidtonesHighlights>(out var smhSh)) smhSh.shadows.value = ColorToVec(value);
                break;
            case ColorPropertyTarget.ShadowsMidtonesHighlightsMidtones:
                if (profile != null && profile.TryGet<ShadowsMidtonesHighlights>(out var smhMi)) smhMi.midtones.value = ColorToVec(value);
                break;
            case ColorPropertyTarget.ShadowsMidtonesHighlightsHighlights:
                if (profile != null && profile.TryGet<ShadowsMidtonesHighlights>(out var smhHi)) smhHi.highlights.value = ColorToVec(value);
                break;
        }
    }
}
