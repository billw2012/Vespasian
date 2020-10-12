﻿using UnityEditor;
using UnityEngine;

public class CameraPostEffectRadius : MonoBehaviour
{
    public float radius = 20;
    [Tooltip("Ratio of effect area to screen at which effects will be completely off"), Range(0, 1)]
    public float minCoverage = 0.1f;
    [Tooltip("Ratio of effect area to screen at which effects will be completely on"), Range(0, 1)]
    public float maxCoverage = 0.5f;

    public new Camera camera;

    public PostEffect effect;

    bool wasInRange = false;

    // Start is called before the first frame update
    void Start()
    {
        if(this.camera == null)
        {
            this.camera = Camera.main;
        }

        this.effect.Init();
    }

    // Update is called once per frame
    void Update()
    {
        var screenRect = this.camera.WorldSpaceRect();
        float coverage = MathX.RectCircleOverlap(screenRect, this.transform.position, this.radius * this.transform.lossyScale.x);
        bool inRange = coverage > 0;
        // This makes sure we only update when we are in range, and once to completely revert them
        // when we move out of range
        if (this.wasInRange || inRange)
        {
            float t = Mathf.Clamp01(Mathf.InverseLerp(this.minCoverage, this.maxCoverage, coverage / (screenRect.width * screenRect.height)));
            this.effect.Update(t);
        }
        this.wasInRange = inRange;
    }

    void OnDestroy()
    {
        this.effect.ResetSettings();
    }


#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Handles.color = UnityEngine.Color.yellow;
        Handles.matrix = this.transform.localToWorldMatrix;
        Handles.DrawWireDisc(Vector3.zero, Vector3.forward, this.radius);
        GUIUtils.Label(Vector3.up * this.radius, "Postfx");
    }
#endif
}
