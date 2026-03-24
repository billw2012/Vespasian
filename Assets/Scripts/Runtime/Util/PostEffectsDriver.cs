using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Serializable helper that drives a set of post-processing parameters
/// toward target values (derived from a 0-1 input t) using SmoothDamp.
/// Compose this into any MonoBehaviour that wants curve-driven post effects.
/// Also owns the static float/color get-set utilities and property-target enums
/// (previously in PostEffect).
/// </summary>
[Serializable]
public class PostEffectsDriver
{
    // ── Enums ─────────────────────────────────────────────────────────────────

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

    // ── Config types ──────────────────────────────────────────────────────────

    [Serializable]
    public class Config
    {
        public FloatPropertyTarget parameter;
        [Tooltip("Parameter value when t = 0")]
        public float startValue;
        [Tooltip("Parameter value when t = 1 — can be lower than startValue")]
        public float endValue;
        [Tooltip("Blends between startValue and the curve-driven value (0 = no effect, 1 = full)")]
        [Range(0f, 2f)]
        public float strength = 1f;
        [Tooltip("Maps t [0, 1] to a position between startValue and endValue")]
        public AnimationCurve responseCurve = AnimationCurve.Linear(0, 0, 1, 1);

        [NonSerialized] public float currentValue;
        [NonSerialized] public float velocity;
    }

    [Serializable]
    public class ColorConfig
    {
        public ColorPropertyTarget parameter;
        [Tooltip("Parameter value when t = 0")]
        public Color startValue = Color.white;
        [Tooltip("Parameter value when t = 1")]
        public Color endValue = Color.white;
        [Tooltip("Blends between startValue and the curve-driven value (0 = no effect, 1 = full)")]
        [Range(0f, 2f)]
        public float strength = 1f;
        [Tooltip("Maps t [0, 1] to a position between startValue and endValue")]
        public AnimationCurve responseCurve = AnimationCurve.Linear(0, 0, 1, 1);

        [NonSerialized] public Color currentValue;
        [NonSerialized] public float velR, velG, velB, velA;
    }

    // ── Driver fields ─────────────────────────────────────────────────────────

    [Tooltip("SmoothDamp time for all parameters — lower = snappier, higher = slower")]
    public float smoothTime = 0.5f;

    public Config[] configs;
    public ColorConfig[] colorConfigs;

    private Volume m_Volume;

    // ── Instance API ──────────────────────────────────────────────────────────

    /// <summary>Call once (e.g. in Start) to bind the Volume and read initial values.</summary>
    public void Init(Volume volume)
    {
        m_Volume = volume;
        if (m_Volume == null) return;
        var profile = m_Volume.profile;
        if (configs != null)
            foreach (var config in configs)
                config.currentValue = GetFloat(profile, config.parameter);
        if (colorConfigs != null)
            foreach (var config in colorConfigs)
                config.currentValue = GetColor(profile, config.parameter);
    }

    /// <summary>Call every frame with a normalised t [0, 1] to animate parameters.</summary>
    public void Update(float t)
    {
        if (m_Volume == null) return;
        var profile = m_Volume.profile;

        if (configs != null)
        {
            foreach (var config in configs)
            {
                float curveT     = config.responseCurve.Evaluate(t);
                float fullTarget = Mathf.Lerp(config.startValue, config.endValue, curveT);
                float target     = Mathf.Lerp(config.startValue, fullTarget, config.strength);
                config.currentValue = Mathf.SmoothDamp(config.currentValue, target, ref config.velocity, smoothTime);
                SetFloat(profile, config.parameter, config.currentValue);
            }
        }

        if (colorConfigs != null)
        {
            foreach (var config in colorConfigs)
            {
                float curveT     = config.responseCurve.Evaluate(t);
                Color fullTarget = Color.Lerp(config.startValue, config.endValue, curveT);
                Color target     = Color.Lerp(config.startValue, fullTarget, config.strength);
                Color cur        = config.currentValue;
                cur.r = Mathf.SmoothDamp(cur.r, target.r, ref config.velR, smoothTime);
                cur.g = Mathf.SmoothDamp(cur.g, target.g, ref config.velG, smoothTime);
                cur.b = Mathf.SmoothDamp(cur.b, target.b, ref config.velB, smoothTime);
                cur.a = Mathf.SmoothDamp(cur.a, target.a, ref config.velA, smoothTime);
                config.currentValue = cur;
                SetColor(profile, config.parameter, cur);
            }
        }
    }

    /// <summary>Restore all parameters to their startValues (call from OnDestroy).</summary>
    public void Reset()
    {
        if (m_Volume == null) return;
        var profile = m_Volume.profile;
        if (configs != null)
            foreach (var config in configs)
                SetFloat(profile, config.parameter, config.startValue);
        if (colorConfigs != null)
            foreach (var config in colorConfigs)
                SetColor(profile, config.parameter, config.startValue);
    }

    // ── Static float utilities ────────────────────────────────────────────────

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

    // ── Static color utilities ────────────────────────────────────────────────

    public static Color GetColor(VolumeProfile profile, ColorPropertyTarget t)
    {
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

    public static void SetColor(VolumeProfile profile, ColorPropertyTarget t, Color value)
    {
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

    private static Color VecToColor(Vector4 v) => new Color(v.x, v.y, v.z, v.w);
    private static Vector4 ColorToVec(Color c) => new Vector4(c.r, c.g, c.b, c.a);
}
