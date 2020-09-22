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

    public Scannable scanTarget;

    #region Objective implementation
    public override Transform target => this.scanTarget.originTransform;
    public override float radius => this.scanTarget.range;
    public override float amountRequired => 1;
    public override float amountDone => this.scanTarget.scanProgress;
    public override bool required => this.makeRequired;
    public override bool active => this.scanTarget.scanning;
    public override string debugName => "Scan";
    public override Color color => Color.yellow;
    #endregion
}
