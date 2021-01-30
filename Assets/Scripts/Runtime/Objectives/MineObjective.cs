using UnityEngine;

public class MineObjective : PositionalObjective
{
    public bool makeRequired;

    protected override void UpdateObjective() {}

    public Mineable mineTarget;

    #region Objective implementation
    public override Transform target => this.mineTarget.originTransform;
    public override float radius => this.mineTarget.range;
    public override float amountRequired => 1;
    public override float amountDone => this.mineTarget.miningProgress;
    public override bool required => this.makeRequired;
    public override bool active => this.mineTarget.beingMined;
    public override bool failed => this.mineTarget.destroyed && !this.complete;
    public override string debugName => "Mine";
    public override Color color => Color.black;
    #endregion
}
