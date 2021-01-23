using IngameDebugConsole;
using System;
using System.Collections.Generic;
using System.Linq;
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

    public Animator animator = null;

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

        if (this.animator != null)
        {
            this.animator.SetFloat("Forward", this.canThrust ? this.thrust.y : 0);
            this.animator.SetFloat("Right", this.canThrust ? this.thrust.x : 0);
        }

        //bool thrusting = canThrust && this.thrust.magnitude > 0;
        //bool wasThrusting = this.prevThrust.magnitude > 0;

        if (this.canThrust && this.thrust.y > 0 && this.prevThrust.y <= 0)
        {
            this.rearAudio.FadeIn(0.01f);
        }
        else if ((!this.canThrust || this.thrust.y <= 0) && this.prevThrust.y > 0)
        {
            this.rearAudio.FadeOut(0.3f);
        }
        if (this.canThrust && this.thrust.y < 0 && this.prevThrust.y >= 0)
        {
            this.frontAudio.FadeIn(0.01f);
        }
        else if ((!this.canThrust || this.thrust.y >= 0) && this.prevThrust.y < 0)
        {
            this.frontAudio.FadeOut(0.2f);
        }

        if (this.canThrust && this.thrust.x != 0 && this.prevThrust.x == 0)
        {
            this.thrusterAudio.FadeIn(0.01f);
        }
        else if ((!this.canThrust || this.thrust.x == 0) && this.prevThrust.x != 0)
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
        
        Debug.DrawLine(this.transform.position, this.transform.position + this.transform.localToWorldMatrix.MultiplyVector(this.thrust), Color.green);
    }

    private void FixedUpdate()
    {
        if(this.canThrust)
        {
            var force = Vector3.zero;
            var forward = this.transform.up; //this.movement.velocity.normalized;
            var right = this.transform.right; //-(Vector3)Vector2.Perpendicular(forward);

            force += forward * this.thrust.y;
            force += right * this.thrust.x;

            float thrustTotal = Mathf.Abs(this.thrust.x) + Mathf.Abs(this.thrust.y);

            this.UseFuel(thrustTotal * Time.fixedDeltaTime * this.constants.FuelUse);

            this.movement.AddForce(force);
        }
    }

    public IEnumerable<FuelTankComponent> allTanks => this.GetComponentsInChildren<FuelTankComponent>().Where(t => t.enabled);
    public IEnumerable<FuelTankComponent> refillableTanks => this.allTanks.Where(f => f.refillable);
    public IEnumerable<FuelTankComponent> nonRefillableTanks => this.allTanks.Where(f => !f.refillable).Reverse();
    
    public float fuel => this.allTanks.Select(e => e.fuel).Sum();
    public float refillableFuel => this.refillableTanks.Select(f => f.fuel).Sum();
    public float refillableMaxFuel => this.refillableTanks.Select(f => f.maxFuel).Sum();
    public bool canRefill => this.refillableFuel != this.refillableMaxFuel;
    public bool canThrust => this.fuel > 0;
    
    public void AddFuel(float amount)
    {
        foreach (var tank in this.refillableTanks.Where(t => !t.fullTank))
        {
            if (amount <= 0)
                break;
            amount = tank.AddFuelWithRemainder(amount);
        }
    }

    public IList<(FuelTankComponent tank, float amount)> GetFuelTankUsage(float amount)
    {
        var usage = new List<(FuelTankComponent tank, float amount)>();

        // Use fuel in non-refillable tanks first (maybe we shouldn't?), and from most empty tank first
        foreach (var tank in this.nonRefillableTanks.Concat(this.refillableTanks))
        {
            if (amount <= 0)
                break;
            (float removed, float remainder) = tank.GetRemoveFuelRemainder(amount);
            if (removed > 0)
            {
                usage.Add((tank, removed));
            }
            amount = remainder;
        }

        return usage;
    }
    
    public float GetFuelTankUsage(FuelTankComponent tank, float amount) => this.GetFuelTankUsage(amount).FirstOrDefault(ta => tank == ta.tank).amount;
    
    public void UseFuel(float amount)
    {
        // Use fuel in non-refillable tanks first (maybe we shouldn't?), and from most empty tank first
        foreach (var (tank, tankAmount) in this.GetFuelTankUsage(amount))
        {
            tank.RemoveFuelWithRemainder(tankAmount);
        }
    }
    
    private static FuelTankComponent GetPlayerEngineComponent() =>
        FindObjectOfType<PlayerController>()?.GetComponentInChildren<FuelTankComponent>();
    
    [ConsoleMethod("player.ship.setfuel", "Set fuel of the players ship")]
    public static void DebugSetPlayerShipFuel(float newFuel)
    {
        var playerEngineComponent = GetPlayerEngineComponent();
        if (playerEngineComponent != null)
        {
            playerEngineComponent.fuel = Mathf.Clamp(newFuel, 0, playerEngineComponent.maxFuel);
        }
    }
}