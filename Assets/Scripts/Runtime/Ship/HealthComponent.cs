using IngameDebugConsole;
using UnityEngine;
using UnityEngine.Events;

public class HealthComponent : MonoBehaviour
{
    [SerializeField]
    private ParticleSystem[] damageParticleSystems = null;
    [SerializeField]
    private GameObject explosionPrefab = null;

    public enum KilledAction
    {
        None,
        Destroy,
        Deactivate
    }
    public KilledAction actionOnKilled = KilledAction.None;
    
    [Tooltip("Hull strength"), Range(0, 3)]
    public float maxHullHP = 1f;

    [Tooltip("Is damage allowed?")]
    public bool allowDamage = true;

    public UnityEvent onKilled;
    
    public float hull => this.hullHP / this.maxHullHP;
    public float shield {
        get {
            var shieldComponent = this.GetComponentInChildren<ShieldComponent>();
            return shieldComponent == null ? 0 : shieldComponent.shield;
        }
    }

    public bool isDamaged => this.hullHP < this.maxHullHP;
    public float damagedHP => this.maxHullHP - this.hullHP;
    

    private float hullHP;
    private float previousHull = 1;
    private Vector3 lastDamageDirection;
    private float damageRate = 0;
    private float damageRateVelocity = 0;

    private void Start()
    {
        this.hullHP = this.maxHullHP;

        foreach (var dpfx in this.damageParticleSystems)
        {
            dpfx.SetEmissionEnabled(false);
        }
    }

    private void Update()
    {
        this.SetTakingDamage((this.previousHull - this.hull) / Time.deltaTime, this.lastDamageDirection);
        this.previousHull = this.hull;
    }

    public void SetTakingDamage(float damageRate, Vector3 direction)
    {
        this.damageRate = Mathf.Max(this.damageRate, damageRate);
        foreach (var dpfx in this.damageParticleSystems)
        {
            dpfx.SetEmissionEnabled(this.damageRate > 0);
            if (this.damageRate > 0)
            {
                dpfx.transform.rotation = Quaternion.FromToRotation(Vector3.up, direction);
                var emission = dpfx.emission;
                emission.rateOverTimeMultiplier = this.damageRate * 100;
            }
        }

        this.damageRate = Mathf.SmoothDamp(this.damageRate, 0, ref this.damageRateVelocity, 1f, 0.1f, Time.deltaTime);
    }

    public void AddDamage(float amount, Vector3 direction)
    {
        if(!this.allowDamage || this.hullHP == 0)
        {
            return;
        }

        var shieldComponent = this.GetComponentInChildren<ShieldComponent>();
        if(shieldComponent != null)
        {
            amount = shieldComponent.AddDamage(amount);
        }
        this.hullHP = Mathf.Clamp(this.hullHP - amount, 0, this.maxHullHP);

        if (amount > 0)
        {
            this.lastDamageDirection = direction.normalized;
        }

        if (this.hull == 0)
        {
            this.Kill();
        }
    }

    public void Kill()
    {
        var explosion = Instantiate(this.explosionPrefab, this.transform.position, Quaternion.identity);

        var explosionMovement = explosion.GetComponent<SimpleBallisticMovement>();
        var simMovement = this.GetComponent<SimMovement>();
        if (explosionMovement != null && simMovement != null)
        {
            explosionMovement.velocity = simMovement.velocity;
        }

        this.hullHP = 0;
        this.onKilled?.Invoke();

        switch (this.actionOnKilled)
        {
            case KilledAction.Deactivate:
                this.gameObject.SetActive(false);
                break;
            case KilledAction.Destroy:
                Destroy(this.gameObject);
                break;
        }
    }

    public void AddHull(float amount) => this.hullHP = Mathf.Clamp(this.hullHP + amount, 0, this.maxHullHP);

    public void FullyRepairHull() => this.hullHP = this.maxHullHP;

    private static HealthComponent GetPlayerHealthComponent() =>
        FindObjectOfType<PlayerController>()?.GetComponentInChildren<HealthComponent>();
    
    [ConsoleMethod("player.ship.sethull", "Set hull of the players ship (0 - 1)")]
    public static void DebugSetPlayerShipHull(float newHull)
    {
        var playerHealthComponent = GetPlayerHealthComponent();
        if (playerHealthComponent != null)
        {
            playerHealthComponent.hullHP = Mathf.Clamp(newHull, 0, 1) * playerHealthComponent.maxHullHP;
        }
    }
}
