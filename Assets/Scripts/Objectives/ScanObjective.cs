using Pixelplacement;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(Scannable))]
public class ScanObjective : PositionalObjective
{
    public bool makeRequired;

    protected override void UpdateObjective() {}

    Scannable scannable => this.GetComponent<Scannable>();

    #region Objective implementation
    public override Transform target => this.scannable.effectSourceTransform;
    public override float radius => this.scannable.maxRadius;
    public override float amountRequired => 1;
    public override float amountDone => this.scannable.scanProgress;
    public override bool required => this.makeRequired;
    public override bool active => this.scannable.scanning;
    public override string debugName => "Scan";
    public override Color color => Color.yellow;
    #endregion
}
