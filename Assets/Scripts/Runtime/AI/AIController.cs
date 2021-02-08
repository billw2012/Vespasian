using UnityEngine;
using System.Linq;
using UnityEngine.SocialPlatforms;
using System;
using System.Collections.Generic;
using UnityEditor;

/// <summary>
/// Represents the players control of some in game entity (typically a ship).
/// Specifically:
/// - converts player input into thrust applied to the EngineController
/// - provides simplified interface to common player ship functions
/// - registers debug console functions for player
/// - handles weapons firing
/// </summary>
public class AIController : ControllerBase
{
    private bool targetVelocityEnabled;
    private Vector2 targetVelocity;

    public void SetTargetVelocity(Vector2 targetVelocity)
    {
        this.targetVelocityEnabled = true;
        this.targetVelocity = targetVelocity;
    }

    public void DisableTargetVelocity() => this.targetVelocityEnabled = false;

    //private void Awake()
    //{
        //base.Awake();
    //}

    private void Update()
    {
        if (this.targetVelocityEnabled)
        {
            var currentVelocity = (Vector2)this.GetComponent<SimMovement>().velocity;
            var globalThrustVector = this.targetVelocity - currentVelocity;
            this.SetThrustGlobal(globalThrustVector);
            //Debug.DrawLine(this.transform.position, this.transform.position + (Vector3)this.targetVelocity, Color.yellow);
            //Debug.DrawLine(this.transform.position, this.transform.position + (Vector3)globalThrustVector, Color.magenta);
        }
        else
        {
            this.SetThrustGlobal(Vector2.zero);
        }

        this.UpdateFiring();
    }




    // Weapons subsystem

    private GameObject FindNearestEnemy()
    {
        var thisFaction = this.faction;
        var allTargets = ComponentCache.FindObjectsOfType<ControllerBase>()
            .Where(i => i.faction != thisFaction)
            .ToArray();
        
        if (allTargets.Length == 0)
            return null;

        var thisPos = this.transform.position;

        var nearestTarget = allTargets
            .Select(i => (obj: i, dist: Vector3.Distance(thisPos, i.transform.position)))
            .OrderBy(i => i.dist)
            .FirstOrDefault().obj.gameObject;

        return nearestTarget;
    }

    private WeaponComponentBase SelectBestWeapon(float rangeToTarget)
    {
        var allWeapons = this.weaponController.GetAllWeapons();
        var bestWeapon = allWeapons.FirstOrDefault(i =>
            i.preferredFiringRangeMin < rangeToTarget &&
            i.preferredFiringRangeMax > rangeToTarget);
        return bestWeapon;
    }

    // Calculates a firing solution for this situation:
    // Our ship and target ship are moving linearly
    // Projectile has a fixed start velocity
    // Returns Vector3.zero if there is no solution
    private static Vector3 CalculateFiringVector(Vector3 shipPos, Vector3 shipVel, Vector3 targetPos, Vector3 targetVel, float projVelAbs)
    {
        // Special value for instant directional weapons
        if (projVelAbs <= 0)
            return (targetPos - shipPos).normalized;

        var targetPosRel = targetPos - shipPos;
        var targetVelRel = targetVel - shipVel;
        float targetVelRelAbs = targetVelRel.magnitude;
        float targetPosRelAbs = targetPosRel.magnitude;
        float targetVelPosDotProduct = Vector3.Dot(targetVelRel, targetPosRel);
        float d = 4.0f * (targetVelPosDotProduct * targetVelPosDotProduct - (targetVelRelAbs * targetVelRelAbs - projVelAbs * projVelAbs) * targetPosRelAbs * targetPosRelAbs);

        if (d < 0.0f)
        {
            return Vector3.zero; // No solution
        }
        else
        {
            float timeImpact = 0;
            float b = 2 * targetVelPosDotProduct;
            float a = targetVelRelAbs * targetVelRelAbs - projVelAbs * projVelAbs;
            if (d == 0.0f)
            {
                timeImpact = -b / (2.0f * a);
            }
            else
            {
                // We have two roots, we must find the smallest positive root
                float sqrtd = Mathf.Sqrt(d);
                float timeImpact0 = (-b + sqrtd) / (2.0f * a);
                float timeImpact1 = (-b - sqrtd) / (2.0f * a);
                // Choose the smallest positive root
                if (timeImpact0 > 0 && timeImpact0 < timeImpact1)
                    timeImpact = timeImpact0;
                else if (timeImpact1 > 0 && timeImpact1 < timeImpact0)
                    timeImpact = timeImpact1;
                else if (timeImpact1 > 0 && timeImpact0 < 0)
                    timeImpact = timeImpact1;
                else if (timeImpact0 > 0 && timeImpact1 < 0)
                    timeImpact = timeImpact0;
            }

            if (timeImpact == 0)
                return new Vector3(1, 0, 0); // It means we are directly at target, we don't care where to fire then

            var firingVector = (1 / projVelAbs) * (targetVelRel + targetPosRel / timeImpact); ;
            return firingVector; //.normalized;
        }
    }

    private float timerFindEnemy = 0;
    private float timerUpdateFiringVector = 0;
    private GameObject currentTarget = null;
    private WeaponComponentBase currentWeapon = null;
    private Vector3 currentFireVector;

    private void UpdateFiring()
    {
        if (this.weaponController != null)
        {
            this.timerFindEnemy += Time.deltaTime;
            if (this.timerFindEnemy > 4.0f)
            {
                this.currentTarget = this.FindNearestEnemy();
                if (this.currentTarget != null)
                {
                    var targetPos = this.currentTarget.transform.position;
                    float distance = Vector3.Distance(targetPos, this.transform.position);
                    this.currentWeapon = this.SelectBestWeapon(distance);
                }
                this.timerFindEnemy = 0;
            }

            // This is called on each frame to keep firing
            if (this.currentTarget != null &&
                this.currentWeapon != null)
            {
                this.timerUpdateFiringVector += Time.deltaTime;
                if (this.timerUpdateFiringVector > 2.0f)
                {
                    var shipVel = this.GetComponent<SimMovement>().velocity;
                    var targetVel = this.currentTarget.GetComponent<SimMovement>().velocity;
                    this.currentFireVector = CalculateFiringVector(this.transform.position, shipVel, this.currentTarget.transform.position, targetVel, this.currentWeapon.projectileStartVelocity);
                    this.timerUpdateFiringVector = 0;
                }

                if (this.currentFireVector != Vector3.zero)
                    this.currentWeapon.FireAt(this.currentFireVector);
            }
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        string textWeapon = this.currentWeapon != null ? this.currentWeapon.upgradeDef.name : "null";
        string textTarget = this.currentTarget != null ? this.currentTarget.ToString() : "null";
        string text = $"Tgt: {textTarget}\nWep: {textWeapon}\nFire vec: {this.currentFireVector}";

        Handles.color = Color.cyan;
        var labelPos = this.transform.position + new Vector3(0, -0.6f, 0);
        Handles.Label(labelPos, text);
    }
#endif
}
