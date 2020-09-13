using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class CameraPostEffect : MonoBehaviour
{
    [Range(0, 20)]
    public float innerRadius = 1;
    [Range(0, 20)]
    public float falloffRange = 3;

    public Transform cameraTransform;
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

    // Start is called before the first frame update
    void Start()
    {
        if(this.cameraTransform == null)
        {
            this.cameraTransform = Camera.main.transform;
        }

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

    bool wasInRange = false;
    // Update is called once per frame
    void Update()
    {
        float distance = ((Vector2)this.transform.worldToLocalMatrix.MultiplyPoint(this.cameraTransform.position)).magnitude;
        bool inRange = distance < this.innerRadius + this.falloffRange;
        // This makes sure we only update when we are in range, and once to completely revert them
        // when we move out of range
        if (this.wasInRange || inRange)
        {
            float t = Mathf.Clamp01(Mathf.InverseLerp(this.innerRadius + this.falloffRange, this.innerRadius, distance));

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
        this.wasInRange = inRange;
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Handles.color = UnityEngine.Color.yellow;
        Handles.matrix = this.transform.localToWorldMatrix;
        Handles.DrawWireDisc(Vector3.zero, Vector3.forward, this.innerRadius);
        Handles.DrawWireDisc(Vector3.zero, Vector3.forward, this.innerRadius + this.falloffRange);
        Handles.Label(Vector3.up * this.innerRadius, "Postfx");
    }
#endif
}
