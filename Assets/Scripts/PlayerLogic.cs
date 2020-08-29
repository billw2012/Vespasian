using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;

public class PlayerLogic : MonoBehaviour
{
    public GameConstants constants;

    public ParticleSystem frontThruster;
    public ParticleSystem rearThruster;
    public ParticleSystem rightThruster;
    public ParticleSystem leftThruster;

    [HideInInspector]
    public Vector3 velocity = Vector3.zero;

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
    public float fuelStart = 1.0f; // To be set in the editor

    public enum FlyingState {
        Aiming, // We are aiming at start of the game
        Flying  // We have been launched and are flying already
    };

    [HideInInspector]
    public FlyingState state = FlyingState.Aiming;

    [HideInInspector]
    // Tracks correct simulated position, as rigid body is not perfectly matching the SimManager generated paths
    public Vector3 simPosition;

    bool canThrust => this.velocity.magnitude != 0 && this.fuelCurrent > 0;

    GravitySource[] gravitySources;

    void OnValidate()
    {
        Assert.IsNotNull(this.constants);
        Assert.IsNotNull(this.frontThruster);
        Assert.IsNotNull(this.rearThruster);
        Assert.IsNotNull(this.rightThruster);
        Assert.IsNotNull(this.leftThruster);
    }

    void Start()
    {
        this.OnValidate();

        this.fuelCurrent = this.fuelStart;

        this.rearThruster.SetEmissionEnabled(false);
        this.frontThruster.SetEmissionEnabled(false);
        this.rightThruster.SetEmissionEnabled(false);
        this.leftThruster.SetEmissionEnabled(false);

        this.thrustInputJoystick = Vector2.zero;
        this.finalThrust = Vector2.zero;
        this.state = FlyingState.Aiming;

        this.gravitySources = GravitySource.All();
        this.simPosition = this.transform.position;
    }

    void Update()
    {
        if (this.state == FlyingState.Flying)
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
    }

    public void SimUpdate()
    {
        if (this.state == FlyingState.Flying)
        {
            var force = this.GetForce(this.simPosition);

            if (this.canThrust)
            {
                var forward = this.velocity.normalized;
                var right = -(Vector3)Vector2.Perpendicular(forward);

                force += forward * this.finalThrust.y;
                force += right * this.finalThrust.x;

                float thrustTotal = Mathf.Abs(this.finalThrust.x) + Mathf.Abs(this.finalThrust.y);
                this.AddFuel(-thrustTotal * Time.fixedDeltaTime * this.constants.FuelUse);
            }

            this.velocity += force * Time.fixedDeltaTime;
            this.simPosition += this.velocity * Time.fixedDeltaTime;

            var rigidBody = this.GetComponent<Rigidbody>();
            this.GetComponent<Rigidbody>().MovePosition(this.simPosition);

            // TODO: rotation needs to be smoothed, but this commented out method results in rotation
            // while following the current orbit lagging.
            // (perhaps even limiting them to the same magnitude?)
            // Smooth rotation slightly to avoid random flipping. Smoothing should not be noticeable in
            // normal play.
            //var desiredRot = Quaternion.FromToRotation(Vector3.up, this.velocity).eulerAngles.z;
            //var currentRot = rigidBody.rotation.eulerAngles.z;
            //var smoothedRot = Mathf.SmoothDampAngle(currentRot, desiredRot, ref this.rotVelocity, 0.01f, 90);
            //rigidBody.MoveRotation(Quaternion.AngleAxis(smoothedRot, Vector3.forward));

            rigidBody.MoveRotation(Quaternion.FromToRotation(Vector3.up, this.velocity));
        }
    }
    public void AddFuel(float amount)
    {
        this.fuelCurrent = Mathf.Clamp(this.fuelCurrent + amount, 0, 1.1f * this.fuelStart);
    }

    Vector3 GetForce(Vector3 pos)
    {
        var force = Vector3.zero;
        foreach(var g in this.gravitySources)
        {
            force += GravityParameters.CalculateForce(pos, g.position, g.parameters.mass, this.constants.GravitationalConstant);
        }
        return force;
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