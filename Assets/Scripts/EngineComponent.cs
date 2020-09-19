using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SimMovement))]
public class EngineComponent : MonoBehaviour
{
    public GameConstants constants;

    public List<ParticleSystem> frontThrusters;
    public List<ParticleSystem> rearThrusters;
    public List<ParticleSystem> rightThrusters;
    public List<ParticleSystem> leftThrusters;
    //public List<ParticleSystem> scoopEffects;
    // public List<ParticleSystem> miningEffects;

    public Animator animator;

    // Final calculated object relative thrust value
    // x is +right/-left, y is +forward/-backward
    [HideInInspector]
    public Vector2 thrust = Vector2.zero;

    [HideInInspector]
    public float fuelCurrent = 1;
    public float fuelStart = 1f; // To be set in the editor

    SimMovement movement;

    bool canThrust => this.movement.velocity.magnitude != 0 && this.fuelCurrent > 0;

    void Awake()
    {
        this.movement = this.GetComponent<SimMovement>();

        this.fuelCurrent = this.fuelStart;

        this.rearThrusters.ForEach(t => t.SetEmissionEnabled(false));
        this.frontThrusters.ForEach(t => t.SetEmissionEnabled(false));
        this.rightThrusters.ForEach(t => t.SetEmissionEnabled(false));
        this.leftThrusters.ForEach(t => t.SetEmissionEnabled(false));

        this.thrust = Vector2.zero;
    }

    // Use LateUpdate to ensure the thrust is calculated already
    void LateUpdate()
    {
        void SetThrusterFX(ParticleSystem pfx, bool enabled, float thrust)
        {
            pfx.SetEmissionEnabled(this.canThrust && enabled);
            const float RateOverTimeMax = 100;
            pfx.SetEmissionRateOverTimeMultiplier(RateOverTimeMax * Mathf.Abs(thrust));
        }

        // Accel/decel
        this.rearThrusters.ForEach(t => SetThrusterFX(t, this.thrust.y > 0, this.thrust.y));
        this.frontThrusters.ForEach(t => SetThrusterFX(t, this.thrust.y < 0, this.thrust.y));

        // Right/left thrusters
        this.rightThrusters.ForEach(t => SetThrusterFX(t, this.thrust.x < 0, this.thrust.x));
        this.leftThrusters.ForEach(t => SetThrusterFX(t, this.thrust.x > 0, this.thrust.x));

        this.animator.SetFloat("Forward", this.thrust.y);
        this.animator.SetFloat("Right", this.thrust.x);
    }

    void FixedUpdate()
    {
        var force = Vector3.zero;
        if (this.canThrust)
        {
            var forward = this.movement.velocity.normalized;
            var right = -(Vector3)Vector2.Perpendicular(forward);

            force += forward * this.thrust.y;
            force += right * this.thrust.x;

            float thrustTotal = Mathf.Abs(this.thrust.x) + Mathf.Abs(this.thrust.y);
            this.AddFuel(-thrustTotal * Time.fixedDeltaTime * this.constants.FuelUse);
        }
        this.movement.AddForce(force);
    }

    public void AddFuel(float amount)
    {
        this.fuelCurrent = Mathf.Clamp01(this.fuelCurrent + amount);
    }
}