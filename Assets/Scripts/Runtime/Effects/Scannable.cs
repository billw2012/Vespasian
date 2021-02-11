using System;
using UnityEngine;

public class Scannable : EffectSource
{
    public float scannedObjectRadius;

    [Saved]
    public float scanProgress { get; private set; } = 0;

    public bool scanning { get; private set; } = false;

    public override bool IsComplete() => this.scanProgress >= 1.0f;

    private void Start()
    {
        // Update scanProgress if we already know about this body
        var bodyRef = this.GetComponent<BodyGenerator>()?.BodyRef;
        if (bodyRef != null)
        {
            this.scanProgress = (ComponentCache.FindObjectOfType<PlayerController>()?
                .GetComponent<DataCatalog>()?
                .HaveData(bodyRef, DataMask.All) ?? false) ? 1 : 0;
        }
    }

    private void LateUpdate() => this.scanning = false;

    // Must be called in update!!
    public void Scan(Scanner scanner)
    {
        float scanAdd = this.timeMultipler * Time.deltaTime * scanner.scanRate;
        this.scanProgress = Mathf.Clamp(this.scanProgress + scanAdd, 0, 1);
        this.scanning = true;
        this.Reveal();
    }

    public override Color gizmoColor => Color.yellow;
    public override string debugName => "Scannable";

    public object Save() => this.scanProgress;
    public void Load(object data) => this.scanProgress = (float)data;
};
