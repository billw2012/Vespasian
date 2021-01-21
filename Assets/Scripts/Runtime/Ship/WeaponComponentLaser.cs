
using System;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Instant medium range, medium damage, weapon.
/// </summary>
public class WeaponComponentLaser : WeaponComponentBase
{
    [SerializeField]
    private float damagePerSecond = 0.1f;
    [SerializeField]
    private ParticleSystem laserEffect = null;

    private float firingCycleRemaining;
    private ControllerBase target;
    private float firingCycleFraction => 1f - this.firingCycleRemaining / (base.firingCooldownTime * 0.5f);
    private Vector3 finalOffset;
    
    protected override void Fire(ControllerBase target)
    {
        this.firingCycleRemaining = base.firingCooldownTime * 0.5f;
        this.target = target;
        this.finalOffset = Quaternion.Euler(0, 0, UnityEngine.Random.Range(0, 360)) * Vector3.right * Random.Range(0f, 2f);
    }

    public override void Update()
    {
        base.Update();

        if (this.firingCycleRemaining > 0 && this.target != null)
        {
            var targetPos = target.transform.position + this.finalOffset * this.firingCycleFraction;
            // Debug.DrawLine(this.transform.position, targetPos, Color.green);
            // Debug.DrawLine(targetPos + Vector3.left, targetPos + Vector3.right, Color.green);
            // Debug.DrawLine(targetPos + Vector3.up, targetPos + Vector3.down, Color.green);
            
            float fullDt = Time.deltaTime * this.simulation.tickStep;
            this.target.GetComponent<HealthComponent>().AddDamage(fullDt * this.damagePerSecond,
                targetPos - this.transform.position);

            this.firingCycleRemaining -= fullDt;
            
            this.laserEffect.gameObject.SetActive(true);
            var vectorToTarget = targetPos - this.transform.position;
            this.laserEffect.transform.rotation =
                Quaternion.Euler(0, 0, Vector2.SignedAngle(Vector2.right, vectorToTarget));
            this.laserEffect.transform.localScale = new Vector3(vectorToTarget.magnitude, 1, 1);
        }
        else
        {
            this.laserEffect.gameObject.SetActive(false);
        }
    }
}