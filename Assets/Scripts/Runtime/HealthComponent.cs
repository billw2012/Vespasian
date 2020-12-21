using UnityEngine;
using UnityEngine.Assertions;

public class HealthComponent : MonoBehaviour
{
    public ParticleSystem damageDebris;
    public GameLogic gameLogic;
    
    [Tooltip("Hull strength"), Range(0, 3)]
    public float maxHullHP = 1f;

    [Tooltip("Is damage allowed?")]
    public bool allowDamage = true;

    public float hull => this.hullHP / this.maxHullHP;
    public float shield {
        get {
            var shield = this.GetComponentInChildren<ShieldComponent>();
            return shield == null ? 0 : shield.shield;
        }
    }

    public bool isDamaged => this.hullHP < this.maxHullHP;
    public float damagedHP => this.maxHullHP - this.hullHP;
    

    private float hullHP;
    private float previousHull = 1;
    private Vector3 lastDamageDirection;

    private void Start()
    {
        this.hullHP = this.maxHullHP;

        this.damageDebris.SetEmissionEnabled(false);
    }

    private void Update()
    {
        this.SetTakingDamage((this.previousHull - this.hull) / Time.deltaTime, this.lastDamageDirection);
        this.previousHull = this.hull;
    }

    public void SetTakingDamage(float damageRate, Vector3 direction)
    {
        this.damageDebris.SetEmissionEnabled(damageRate > 0);
        if (damageRate > 0)
        {
            this.damageDebris.transform.rotation = Quaternion.FromToRotation(Vector3.up, direction);
            var emission = this.damageDebris.emission;
            emission.rateOverTimeMultiplier = damageRate * 100;
        }
    }

    public void AddDamage(float amount, Vector3 direction)
    {
        if(!this.allowDamage)
        {
            return;
        }

        var shield = this.GetComponentInChildren<ShieldComponent>();
        if(shield != null)
        {
            amount = shield.AddDamage(amount);
        }
        this.hullHP = Mathf.Clamp(this.hullHP - amount, 0, this.maxHullHP);

        if (amount > 0)
        {
            this.lastDamageDirection = direction;
        }

        if (this.hull == 0)
        {
            this.gameLogic.LoseGameAsync();
        }
    }

    public void AddHull(float amount)
    {
        this.hullHP = Mathf.Clamp(this.hullHP + amount, 0, this.maxHullHP);
    }

    public void FullyRepairHull()
    {
        this.hullHP = this.maxHullHP;
    }
}
