using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Converts thrust into engine behavior, e.g. using fuel, animating, etc.
/// </summary>
public class EngineController : MonoBehaviour
{
    public GameConstants constants;

    public List<ParticleSystem> frontThrusters;
    public List<ParticleSystem> rearThrusters;
    public List<ParticleSystem> rightThrusters;
    public List<ParticleSystem> leftThrusters;

    public Animator animator;

    // Final calculated object relative thrust value
    // x is +right/-left, y is +forward/-backward
    [NonSerialized]
    public Vector2 thrust = Vector2.zero;

    SimMovement movement;


    void Start()
    {
        this.movement = this.GetComponentInParent<SimMovement>();

        this.rearThrusters.ForEach(t => t.SetEmissionEnabled(false));
        this.frontThrusters.ForEach(t => t.SetEmissionEnabled(false));
        this.rightThrusters.ForEach(t => t.SetEmissionEnabled(false));
        this.leftThrusters.ForEach(t => t.SetEmissionEnabled(false));

        this.thrust = Vector2.zero;
    }

    // Use LateUpdate to ensure the thrust is calculated already
    void LateUpdate()
    {
        var engineComponent = this.GetComponentInChildren<EngineComponent>();
        bool canThrust = engineComponent != null && engineComponent.canThrust;

        void SetThrusterFX(ParticleSystem pfx, bool enabled, float thrust)
        {
            pfx.SetEmissionEnabled(canThrust && enabled);
            const float RateOverTimeMax = 100;
            pfx.SetEmissionRateOverTimeMultiplier(RateOverTimeMax * Mathf.Abs(thrust));
        }

        // Accel/decel
        this.rearThrusters.ForEach(t => SetThrusterFX(t, this.thrust.y > 0, this.thrust.y));
        this.frontThrusters.ForEach(t => SetThrusterFX(t, this.thrust.y < 0, this.thrust.y));

        // Right/left thrusters
        this.rightThrusters.ForEach(t => SetThrusterFX(t, this.thrust.x < 0, this.thrust.x));
        this.leftThrusters.ForEach(t => SetThrusterFX(t, this.thrust.x > 0, this.thrust.x));

        this.animator.SetFloat("Forward", canThrust? this.thrust.y : 0);
        this.animator.SetFloat("Right", canThrust? this.thrust.x : 0);
    }

    void FixedUpdate()
    {
        var engineComponent = this.GetComponentInChildren<EngineComponent>();
        if (engineComponent != null && engineComponent.canThrust)
        {
            var force = Vector3.zero;
            var forward = this.movement.velocity.normalized;
            var right = -(Vector3)Vector2.Perpendicular(forward);

            force += forward * this.thrust.y;
            force += right * this.thrust.x;

            float thrustTotal = Mathf.Abs(this.thrust.x) + Mathf.Abs(this.thrust.y);

            engineComponent.UseFuel(thrustTotal * Time.fixedDeltaTime * this.constants.FuelUse);

            this.movement.AddForce(force);
        }
    }
}