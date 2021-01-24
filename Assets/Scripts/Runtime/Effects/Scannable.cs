using System;
using UnityEngine;

public class Scannable : EffectSource
{
    public float scannedObjectRadius;

    [Saved]
    public float scanProgress { get; private set; } = 0;

    public bool scanning { get; private set; } = false;

    public override bool IsComplete() => this.scanProgress >= 1.0f;

    private void LateUpdate()
    {
        this.scanning = false;
    }

    // Must be called in update!!
    public void Scan(Scanner _)
    {
        float scanAdd = this.timeMultipler * Time.deltaTime * 0.2f;
        this.scanProgress = Mathf.Clamp(this.scanProgress + scanAdd, 0, 1);
        this.scanning = true;
        this.Reveal();
    }

    public override Color gizmoColor => Color.yellow;
    public override string debugName => "Scannable";

    public object Save() => this.scanProgress;
    public void Load(object data) { this.scanProgress = (float)data; }
};
