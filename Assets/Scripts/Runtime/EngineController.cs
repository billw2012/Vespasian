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

    //public AudioSource engineStart;
    public FadeableAudio rearAudio;
    public FadeableAudio frontAudio;
    public FadeableAudio thrusterAudio;
    //public AudioSource engineEnd;

    // Final calculated object relative thrust value
    // x is +right/-left, y is +forward/-backward
    [NonSerialized]
    public Vector2 thrust = Vector2.zero;

    private SimMovement movement;
    private Vector2 prevThrust;


    private void Start()
    {
        this.movement = this.GetComponentInParent<SimMovement>();

        this.rearThrusters.ForEach(t => t.SetEmissionEnabled(false));
        this.frontThrusters.ForEach(t => t.SetEmissionEnabled(false));
        this.rightThrusters.ForEach(t => t.SetEmissionEnabled(false));
        this.leftThrusters.ForEach(t => t.SetEmissionEnabled(false));

        this.thrust = this.prevThrust = Vector2.zero;
    }

    // Use LateUpdate to ensure the thrust is calculated already
    private void LateUpdate()
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

        //bool thrusting = canThrust && this.thrust.magnitude > 0;
        //bool wasThrusting = this.prevThrust.magnitude > 0;

        if (canThrust && this.thrust.y > 0 && this.prevThrust.y <= 0)
        {
            this.rearAudio.FadeIn(0.01f);
        }
        else if ((!canThrust || this.thrust.y <= 0) && this.prevThrust.y > 0)
        {
            this.rearAudio.FadeOut(0.3f);
        }
        if (canThrust && this.thrust.y < 0 && this.prevThrust.y >= 0)
        {
            this.frontAudio.FadeIn(0.01f);
        }
        else if ((!canThrust || this.thrust.y >= 0) && this.prevThrust.y < 0)
        {
            this.frontAudio.FadeOut(0.2f);
        }

        if (canThrust && this.thrust.x != 0 && this.prevThrust.x == 0)
        {
            this.thrusterAudio.FadeIn(0.01f);
        }
        else if ((!canThrust || this.thrust.x == 0) && this.prevThrust.x != 0)
        {
            this.thrusterAudio.FadeOut(0.1f);
        }

        //bool thrusterFired = this.thrust.x != 0 && this.prevThrust.x == 0 ||
        //    this.thrust.y != 0 && this.prevThrust.y == 0;
        //if (thrusterFired)
        //{
        //    this.engineStart.Play();
        //}
        //bool thrusterShutdown = this.thrust.x == 0 && this.prevThrust.x != 0 ||
        //    this.thrust.y == 0 && this.prevThrust.y != 0;
        //if (thrusterShutdown)
        //{
        //    this.engineEnd.Play();
        //}
        this.prevThrust = this.thrust;
    }

    private void FixedUpdate()
    {
        // Engine is an upgrade so we don't cache it.
        // TODO: optimize by using UpgradeManager upgrades changed event and caching the EngineComponent.
        var engineComponent = this.GetComponentInChildren<EngineComponent>();
        if (engineComponent != null && engineComponent.canThrust)
        {
            var force = Vector3.zero;
            var forward = this.transform.up; //this.movement.velocity.normalized;
            var right = this.transform.right; //-(Vector3)Vector2.Perpendicular(forward);

            force += forward * this.thrust.y;
            force += right * this.thrust.x;

            float thrustTotal = Mathf.Abs(this.thrust.x) + Mathf.Abs(this.thrust.y);

            engineComponent.UseFuel(thrustTotal * Time.fixedDeltaTime * this.constants.FuelUse);

            this.movement.AddForce(force);
        }
    }
}