using UnityEngine;

public class ControllerBase : MonoBehaviour
{
    public Faction.FactionType faction = Faction.FactionType.None;
    
    protected void SetThrust(Vector2 thrustVector) => this.SetThrust(thrustVector.y, thrustVector.x);
    protected void SetThrustGlobal(Vector2 thrustVector) => this.SetThrust(this.transform.worldToLocalMatrix.MultiplyVector(thrustVector));
    
    private UpgradeComponentProxy<ThrustComponent> thrustComponentProxy;
    private ThrustComponent thrustComponent;
    
    protected void Start()
    {
        this.thrustComponentProxy = this.GetComponent<UpgradeManager>()?.GetProxy<ThrustComponent>();
        if (this.thrustComponentProxy == null)
        {
            this.thrustComponent = this.GetComponentInChildren<ThrustComponent>();
        }
    }

    protected void SetThrust(float forward, float right)
    {
        var engine = this.GetComponent<EngineController>();
        var thrusters = this.thrustComponentProxy?.value ?? this.thrustComponent;
        engine.thrust.y 
            = engine.constants.ThrustForward * thrusters.reverseThrust * Mathf.Clamp(forward, -1, 0)
            + engine.constants.ThrustForward * thrusters.forwardThrust * Mathf.Clamp(forward, 0, 1)
            ;
        engine.thrust.x = engine.constants.ThrustRight * thrusters.lateralThrust * Mathf.Clamp(right, -1, 1);
    }

    public void SetControlled(bool allow)
    {
        this.enabled = allow;
        var simMovement = this.GetComponent<SimMovement>();
        simMovement.enabled = allow;
        foreach (var c in this.GetComponentsInChildren<Collider>())
        {
            c.enabled = allow;
        }
        this.GetComponent<HealthComponent>().allowDamage = allow;
    }
}