using Pixelplacement;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(ScanEffect))]
public class ScanObjective : PositionalObjective
{
    public bool makeRequired;

    protected override void UpdateObjective() {}

    ScanEffect scanEffect => this.GetComponent<ScanEffect>();

    #region Objective implementation
    public override Transform target => this.scanEffect.effector;
    public override float radius => this.scanEffect.maxRadius;
    public override float amountRequired => 1;
    public override float amountDone => this.scanEffect.scanned;
    public override bool required => this.makeRequired;
    public override bool active => this.scanEffect.scanning;
    public override string debugName => "Scan";
    public override Color color => Color.yellow;
    #endregion
}
