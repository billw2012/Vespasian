using UnityEngine;

public class FlybyObjective : PositionalObjective
{
    [Range(0, 30f)]
    public float flybyMaxDistance = 5f;
    public bool makeRequired;

    private float traversed = 0;
    private bool traversing = false;
    private Vector3 lastPos;

    public Transform flybyTarget;

    // Use FixedUpdate as we are tracking position of objects that are updated in FixedUpdate
    protected override void UpdateObjective()
    {
        var player = ComponentCache.FindObjectOfType<PlayerController>();
        if (player == null)
        {
            return;
        }

        this.traversing = this.lastPos != Vector3.zero && Vector2.Distance(player.transform.position, this.target.position) <= this.flybyMaxDistance;
        if (this.traversing)
        {
            this.traversed += Vector2.Distance(player.transform.position, this.lastPos);
        }

        this.lastPos = player.transform.position;
    }

    #region Objective implementation
    public override Transform target => this.flybyTarget;
    public override float radius => this.flybyMaxDistance;
    public override float amountRequired => this.radius; // player must travel within range for this much distance
    public override float amountDone => this.traversed;
    public override bool required => this.makeRequired;
    public override bool active => this.traversing;
    public override string debugName => "Flyby";
    public override Color color => Color.blue;
    #endregion
}
