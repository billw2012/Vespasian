﻿using UnityEngine;

public class Scannable : EffectSource
{
    [HideInInspector]
    public float scanProgress = 0;

    [HideInInspector]
    public bool scanning = false;

    public override bool IsEmpty()
    {
        return this.scanProgress >= 1.0f;
    }

    void LateUpdate()
    {
        this.scanning = false;
    }

    // Must be called in update!!
    public void Scan(Scanner _)
    {
        float scanAdd = this.timeMultipler * Time.deltaTime * 0.2f;
        this.scanProgress = Mathf.Clamp(this.scanProgress + scanAdd, 0, 1);
        this.scanning = true;
    }

    public override Color gizmoColor => Color.yellow;
    public override string debugName => "Scannable";
};