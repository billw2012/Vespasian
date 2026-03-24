using System;
using UnityEngine;
using UnityEngine.Rendering;

public class StarPostEffectsController : MonoBehaviour
{
    [Serializable]
    public class Config
    {
        public PostEffect.FloatPropertyTarget parameter;
        [Tooltip("Parameter value when coverage is at or below minThreshold (t = 0)")]
        public float startValue;
        [Tooltip("Parameter value when coverage is at or above maxThreshold (t = 1) — can be lower than startValue")]
        public float endValue;
        [Tooltip("Blends between startValue and the curve-driven value (0 = no effect, 1 = full)")]
        [Range(0f, 2f)]
        public float strength = 1f;
        [Tooltip("Maps remapped coverage t [0, 1] to a position between startValue and endValue")]
        public AnimationCurve responseCurve = AnimationCurve.Linear(0, 0, 1, 1);

        [NonSerialized] public float currentValue;
        [NonSerialized] public float velocity;
    }

    [Header("Coverage")]
    [Tooltip("Multiplier on BodyLogic.radius for coverage calculation — increase to account for glow")]
    public float starRadiusScale = 1f;
    [Tooltip("Coverage fraction below which no effect is applied (t = 0)")]
    [Range(0f, 1f)]
    public float minThreshold = 0.3f;
    [Tooltip("Coverage fraction at which the curve is fully evaluated (t = 1)")]
    [Range(0f, 1f)]
    public float maxThreshold = 1f;

    [Header("Easing")]
    [Tooltip("SmoothDamp time for all parameters — lower = snappier, higher = slower")]
    public float smoothTime = 0.5f;

    [Header("Effects")]
    public Config[] configs;

    [Header("Debug")]
    public bool debugLog = false;

    private Volume volume;
    private Camera cam;

    private void Start()
    {
        this.volume = GetComponent<Volume>();
        this.cam = Camera.main;

        if (this.volume == null || this.configs == null) return;
        foreach (var config in this.configs)
            config.currentValue = PostEffect.GetFloat(this.volume.profile, config.parameter);
    }

    private void LateUpdate()
    {
        if (this.volume == null || this.cam == null || this.configs == null) return;

        var screenRect = this.cam.WorldSpaceRect();
        float screenArea = screenRect.width * screenRect.height;
        if (screenArea <= 0f) return;

        float totalOverlap = 0f;
        foreach (var star in ComponentCache.FindObjectsOfType<StarLogic>())
        {
            var body = star.GetComponent<BodyLogic>();
            if (body == null) continue;
            totalOverlap += MathX.RectCircleOverlap(screenRect, star.transform.position, body.radius * this.starRadiusScale);
        }

        float coverage = Mathf.Clamp01(totalOverlap / screenArea);
        float t = coverage <= this.minThreshold ? 0f
            : Mathf.Clamp01(Mathf.InverseLerp(this.minThreshold, this.maxThreshold, coverage));

        var profile = this.volume.profile;
        foreach (var config in this.configs)
        {
            float curveT = config.responseCurve.Evaluate(t);
            float fullTarget = Mathf.Lerp(config.startValue, config.endValue, curveT);
            float target = Mathf.Lerp(config.startValue, fullTarget, config.strength);

            config.currentValue = Mathf.SmoothDamp(config.currentValue, target, ref config.velocity, this.smoothTime);
            PostEffect.SetFloat(profile, config.parameter, config.currentValue);

            if (this.debugLog)
                Debug.Log($"[StarCoverage] {config.parameter} coverage={coverage:F4} t={t:F4} curveT={curveT:F4} target={target:F4} current={config.currentValue:F4}");
        }
    }

    private void OnDestroy()
    {
        if (this.volume == null || this.configs == null) return;
        var profile = this.volume.profile;
        foreach (var config in this.configs)
            PostEffect.SetFloat(profile, config.parameter, config.startValue);
    }
}
