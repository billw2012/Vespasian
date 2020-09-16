using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

[RequireComponent(typeof(SimMovement))]
public class PlayerLogic : MonoBehaviour
{
    public GameConstants constants;

    public ParticleSystem frontThruster;
    public ParticleSystem rearThruster;
    public ParticleSystem rightThruster;
    public ParticleSystem leftThruster;
    public ParticleSystem scoopEffect;
    public ParticleSystem miningEffect;

    [HideInInspector]
    // NORMALIZED dimensionless thrust input for joystick
    // x is -1...1 <=> +right/-left, y is -1...1 <=> +forward/-backward
    public Vector2 thrustInputJoystick = Vector2.zero;
    // NORMALIZED dimensionless thrust input for separate axis -1..1 <=> -max...+max
    public float thrustInputForward { get; set; }
    public float thrustInputRight { get; set; }

    // Final calculated thrust value
    // x is +right/-left, y is +forward/-backward
    Vector2 finalThrust = Vector2.zero;

    [HideInInspector]
    public float fuelCurrent = 1;
    public float fuelStart = 1f; // To be set in the editor

    SimMovement movement;

    bool canThrust => this.movement.velocity.magnitude != 0 && this.fuelCurrent > 0;

    void Awake()
    {
        this.OnValidate();

        this.movement = this.GetComponent<SimMovement>();
        Assert.IsNotNull(this.movement);

        this.fuelCurrent = this.fuelStart;

        this.rearThruster.SetEmissionEnabled(false);
        this.frontThruster.SetEmissionEnabled(false);
        this.rightThruster.SetEmissionEnabled(false);
        this.leftThruster.SetEmissionEnabled(false);

        this.thrustInputJoystick = Vector2.zero;
        this.finalThrust = Vector2.zero;
    }

    void OnValidate()
    {
        Assert.IsNotNull(this.constants);
        Assert.IsNotNull(this.frontThruster);
        Assert.IsNotNull(this.rearThruster);
        Assert.IsNotNull(this.rightThruster);
        Assert.IsNotNull(this.leftThruster);
    }

    void Update()
    {
        this.UpdateFinalThrust();

        void SetThrusterFX(ParticleSystem pfx, bool enabled, float thrust)
        {
            pfx.SetEmissionEnabled(this.canThrust && enabled);
            const float RateOverTimeMax = 100;
            pfx.SetEmissionRateOverTimeMultiplier(RateOverTimeMax * Mathf.Abs(thrust));
        }

        // Accel/decel
        SetThrusterFX(this.rearThruster, this.finalThrust.y > 0, this.finalThrust.y);
        SetThrusterFX(this.frontThruster, this.finalThrust.y < 0, this.finalThrust.y);

        // Right/left thrusters
        SetThrusterFX(this.rightThruster, this.finalThrust.x < 0, this.finalThrust.x);
        SetThrusterFX(this.leftThruster, this.finalThrust.x > 0, this.finalThrust.x);
    }

    void FixedUpdate()
    {
        var force = Vector3.zero;
        if (this.canThrust)
        {
            var forward = this.movement.velocity.normalized;
            var right = -(Vector3)Vector2.Perpendicular(forward);

            force += forward * this.finalThrust.y;
            force += right * this.finalThrust.x;

            float thrustTotal = Mathf.Abs(this.finalThrust.x) + Mathf.Abs(this.finalThrust.y);
            this.AddFuel(-thrustTotal * Time.fixedDeltaTime * this.constants.FuelUse);
        }
        this.movement.AddForce(force);
    }

    public void AddFuel(float amount)
    {
        this.fuelCurrent = Mathf.Clamp01(this.fuelCurrent + amount);
    }

    // Calculates final thrust value from various control inputs
    void UpdateFinalThrust()
    {
        // Add thrust from keyboard
        var kbInput = new Vector2(0, 0);
        if (Input.GetKey("w"))
            kbInput.y = 1.0f;
        else if (Input.GetKey("s"))
            kbInput.y = -1.0f;

        if (Input.GetKey("d"))
            kbInput.x = 1.0f;
        else if (Input.GetKey("a"))
            kbInput.x = -1.0f;

        // Convert normalized inputs into final values in (kind of) Newtons
        this.finalThrust.y = this.constants.ThrustForward * Mathf.Clamp(this.thrustInputForward + this.thrustInputJoystick.y + kbInput.y, -1, 1);
        this.finalThrust.x = this.constants.ThrustRight * Mathf.Clamp(this.thrustInputRight + this.thrustInputJoystick.x + kbInput.x, -1, 1); ;
    }
}