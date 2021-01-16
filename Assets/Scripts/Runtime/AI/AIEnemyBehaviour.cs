// unset

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

public class AIEnemyBehaviour : MonoBehaviour, ISimUpdate
{
    [SerializeField]
    private float shipFollowDistance = 3f;

    [SerializeField]
    private WeightedRandom updateInterval = null;

    private SimMovement simMovement;
    private int nextUpdateTick;
    private RandomX rng;
    private Vector3 interceptPos;

    // Possible AI states in priority order
    private enum State
    {
        AvoidCollision,
        AchieveSafeOrbit,
        ReverseOrbit,
        Intercept,
        DirectApproach,
        Follow,
        Idle,
    }
    
    // public 
    
    private void Awake()
    {
        this.simMovement = this.GetComponent<SimMovement>();
        this.rng = new RandomX();
    }
    
    private void Update()
    {
        Debug.DrawLine(this.transform.position, this.interceptPos, Color.red);
    }

    public void SimUpdate(Simulation simulation, int simTick, int timeStep)
    {
        if (simTick >= this.nextUpdateTick)
        {
            var target = FindObjectOfType<PlayerController>().GetComponent<SimMovement>();
            var primary = FindObjectOfType<StarLogic>().GetComponent<GravitySource>();
            
            // We can only approach the target directly if they are in a "global" trajectory, i.e. not in orbit
            // around a planet/moon. Its fine if they are passing through multiple SOIs though as long as one of them is
            // the stars.
            (var targetPos, var targetVel, float followDistance) = this.GetTargetSpec(target, primary);
            this.GetComponent<AIController>().targetVelocity = this.GetVelocityToIntercept(primary, targetPos, targetVel, followDistance);
            this.nextUpdateTick +=  (int)(this.updateInterval.Evaluate(this.rng) / Time.fixedDeltaTime);
        }
    }

    public void SimRefresh(Simulation simulation) {}

    private (Vector3 targetPos, Vector3 targetVel, float followDistance) GetTargetSpec(SimMovement target, GravitySource primary)
    {
        bool targetDirectlyApproachable = !target.sois.Any() || target.sois.Any(s => s.g == primary);
        if (targetDirectlyApproachable)
        {
            return (target.transform.position, target.velocity, this.shipFollowDistance);
        }
        else
        {
            var secondary = target.sois.First().g;
            var secondaryOrbit = secondary.GetComponent<Orbit>();

            // We want to trail outside of the SOI radius so we don't get dragged into the planet
            // TODO: maybe trail at L5? Its slow to catch the player if we do this...
            float soiRadius = OrbitalUtils.SecondarySOIRadius(secondaryOrbit.parameters.semiMajorAxis, secondary.parameters.mass, primary.parameters.mass, secondary.constants.GravitationalConstant, secondary.constants.GravitationalRescaling);

            return (secondaryOrbit.position.position, secondaryOrbit.absoluteVelocity, soiRadius * 5f);
        }
    }

    private Vector3 GetVelocityToIntercept(GravitySource primary, Vector3 targetPos, Vector3 targetVel, float trailDistance)
    {
        // If we are orbiting the same direction as the target we can directly intercept it
        // Otherwise we should reverse our orbit first
        
        // 1. determine predicted target orbit
        var targetOrbit = AnalyticOrbit.FromCartesianStateVector(
            targetPos, targetVel, 
            primary.parameters.mass, primary.constants.GravitationalConstant);
        
        // 1b. determine our orbit
        var ourPosition = this.transform.position;
        var ourVelocity = this.simMovement.velocity;
        var ourOrbit = AnalyticOrbit.FromCartesianStateVector(
            ourPosition, ourVelocity, 
            primary.parameters.mass, primary.constants.GravitationalConstant);

        // WIP
        //if(ourOrbit.periapsis <= )
        
        // if (ourOrbit.directionSign != targetOrbit.directionSign)
        // {
        //     // We aren't orbiting in the same direction, so change direction first
        //     
        // }
        // else
        {
            // We are in the same orbit so lets try and intercept
            
            // 2. calculate a suitable time-of-flight based on distance to target + its orbit characteristics (maybe)
            var tof = Vector3.Distance(ourPosition, targetPos) * 0.5f;

            // 3. calculate the targets position at T+time-of-flight (this is the intercept point)
            // orbits with eccentricty near 1 are unstable in this system, we will just fly to the player instead of predicting position
            float trailTime = trailDistance / targetVel.magnitude;
            this.interceptPos = targetOrbit.isUnstable ? (Vector2)targetPos : targetOrbit.GetPosition(tof - trailTime);

            // 4. solve lambert equation for transfer from current position to this position
            var (Vi, Vf) = GoodingSolver.Solve(primary.constants.GravitationalConstant * primary.parameters.mass,
                ourPosition, this.simMovement.velocity, this.interceptPos, tof, 0);

            return Vi;
        }
    }
}
