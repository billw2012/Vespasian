using UnityEngine;

public class OrbitObjective : PositionalObjective
{
    [Range(0, 30f)]
    public float orbitMaxRadius = 5f;
    [Range(0.25f, 10f)]
    public float requiredOrbits = 1;
    public bool makeRequired;

    public GravitySource orbitTarget;

    private bool isOrbit;
    private Vector2 lastRelativePosition;
    private float performedOrbits = 0;

    protected override void UpdateObjective()
    {
        var player = ComponentCache.FindObjectOfType<PlayerController>();
        if(player == null)
        {
            return;
        }
        var gravity = this.orbitTarget;

        var newRelativePosition = (Vector2)(player.transform.position - this.target.position);
        var velocity = (newRelativePosition - this.lastRelativePosition) / Time.deltaTime;

        float angleDt = Vector2.Angle(this.lastRelativePosition, newRelativePosition);

        this.lastRelativePosition = newRelativePosition;

        // doing: record relative position, use
        this.isOrbit = 
            // In range
            newRelativePosition.magnitude < this.radius &&
            // In an elliptical orbit (must check this before checking semi major axis or we might get divide by zero)
            OrbitalUtils.OrbitDiscriminator(velocity.magnitude, newRelativePosition.magnitude, gravity.parameters.mass, gravity.constants.GravitationalConstant) > 0 &&
            // Orbital major axis is okay
            OrbitalUtils.SemiMajorAxis(velocity.magnitude, newRelativePosition.magnitude, gravity.parameters.mass, gravity.constants.GravitationalConstant) < this.radius;

        if(this.isOrbit)
        {
            this.performedOrbits += angleDt / 360f;
        }
    }

    #region Objective implementation
    public override Transform target => this.orbitTarget.target;
    public override float radius => this.orbitMaxRadius;
    public override float amountRequired => this.requiredOrbits;
    public override float amountDone => this.performedOrbits;
    public override bool required => this.makeRequired;
    public override bool active => this.isOrbit;
    public override string debugName => "Orbit";
    public override Color color => Color.green;
    #endregion
}
