using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;

public class PlayerLogic : MonoBehaviour
{
    [HideInInspector]
    public Vector3 velocity = Vector3.zero;

    [HideInInspector]
    // x is +right/-left, y is +forward/-backward
    public Vector2 manualThrust = Vector2.zero;

    public float forwardThrust { get => this.manualThrust.y; set => this.manualThrust.y = value; }
    public float rightThrust { get => this.manualThrust.x; set => this.manualThrust.x = value; }

    [HideInInspector]
    // x is +right/-left, y is +forward/-backward
    Vector2 finalThrust = Vector2.zero;

    bool canThrust => this.velocity.magnitude != 0 && GameLogic.Instance.remainingFuel > 0;

    public ParticleSystem frontThruster;
    public ParticleSystem rearThruster;
    public ParticleSystem rightThruster;
    public ParticleSystem leftThruster;
    public ParticleSystem damageDebris;

    public enum FlyingState {
        Aiming, // We are aiming at start of the game
        Flying  // We have been launched and are flying already
    };

    public FlyingState state = FlyingState.Aiming;

    static void SetEmissionActive(ParticleSystem pfx, bool enabled)
    {
        var em = pfx.emission;
        if(em.enabled != enabled)
        {
            em.enabled = enabled;
        }
    }

    GravitySource[] gravitySources;
    // Tracks correct simulated position, as rigid body is not perfectly matching the SimManager generated paths
    public Vector3 simPosition;

    void Start()
    {
        SetEmissionActive(this.rearThruster, false);
        SetEmissionActive(this.frontThruster, false);
        SetEmissionActive(this.rightThruster, false);
        SetEmissionActive(this.leftThruster, false);
        SetEmissionActive(this.damageDebris, false);

        this.manualThrust = Vector2.zero;
        this.finalThrust = Vector2.zero;
        this.state = FlyingState.Aiming;

        this.gravitySources = GravitySource.All();
        this.simPosition = this.transform.position;
    }

    Vector3 GetForce(Vector3 pos)
    {
        var force = Vector3.zero;
        foreach(var g in this.gravitySources)
        {
            force += GravityParameters.CalculateForce(pos, g.transform.position, g.parameters.mass);
        }
        return force;
    }

    void UpdateFinalThrust()
    {
        this.finalThrust = this.manualThrust;
        if (Input.GetKey("w"))
            this.finalThrust.y = GameConstants.Instance.ThrustForward;
        else if (Input.GetKey("s"))
            this.finalThrust.y = -GameConstants.Instance.ThrustForward;

        if (Input.GetKey("d"))
            this.finalThrust.x = GameConstants.Instance.ThrustRight;
        else if (Input.GetKey("a"))
            this.finalThrust.x = -GameConstants.Instance.ThrustRight;
    }

    void Update()
    {
        if (this.state == FlyingState.Flying)
        {
            this.UpdateFinalThrust();

            void SetThrusterFX(ParticleSystem pfx, bool enabled, float thrust)
            {
                var thrusterModule = pfx.emission;
                thrusterModule.enabled = this.canThrust && enabled;

                const float RateOverTimeMax = 100;
                thrusterModule.rateOverTimeMultiplier = RateOverTimeMax * Mathf.Abs(thrust);
            }

            // Accel/decel
            SetThrusterFX(this.rearThruster, this.finalThrust.y > 0, this.finalThrust.y);
            SetThrusterFX(this.frontThruster, this.finalThrust.y < 0, this.finalThrust.y);

            // Right/left thrusters
            SetThrusterFX(this.rightThruster, this.finalThrust.x < 0, this.finalThrust.x);
            SetThrusterFX(this.leftThruster, this.finalThrust.x > 0, this.finalThrust.x);
        }
    }

    void FixedUpdate()
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
                GameLogic.Instance.AddFuel(-thrustTotal * Time.fixedDeltaTime * GameConstants.Instance.FuelUse);
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

    public void SetTakingDamage(float damageRate, Vector3 direction)
    {
        SetEmissionActive(this.damageDebris, damageRate > 0);
        if (damageRate > 0)
        {
            this.damageDebris.transform.rotation = Quaternion.FromToRotation(Vector3.up, direction);
            var emission = this.damageDebris.emission;
            emission.rateOverTimeMultiplier = damageRate * 100;
        }
    }
}