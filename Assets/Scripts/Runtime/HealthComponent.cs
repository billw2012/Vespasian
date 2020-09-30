using Pixelplacement;
using Pixelplacement.TweenSystem;
using UnityEngine;
using UnityEngine.Assertions;

public class HealthComponent : MonoBehaviour
{
    public ParticleSystem damageDebris;
    public GameLogic gameLogic;
    
    [Tooltip("Time to fully recharge shield"), Range(1, 30)]
    public float shieldRechargeTime = 10f;
    [Tooltip("Time without damage taken before shield will start recharging"), Range(0, 30)]
    public float shieldRechargeDelay = 5f;

    [Tooltip("Shield strength"), Range(0, 3)]
    public float maxShieldHP = 0.5f;
    [Tooltip("Hull strength"), Range(0, 3)]
    public float maxHullHP = 1f;

    public float hull => this.hullHP / this.maxHullHP;
    public float shield => this.shieldHP / this.maxShieldHP;

    public Transform shieldTransform;
    public MeshRenderer shieldRenderer;

    float hullHP;
    float shieldHP;
    float lastDamageTime = float.MinValue;
    float previousShield = 1;
    float previousHull = 1;
    Vector3 lastDamageDirection;

    void Start()
    {
        this.hullHP = this.maxHullHP;
        this.shieldHP = this.maxShieldHP;

        this.damageDebris.SetEmissionEnabled(false);
    }

    TweenBase activeShieldAnim;
    float shieldFade = 0f;

    void Update()
    {
        this.SetTakingDamage((this.previousHull - this.hull) / Time.deltaTime, this.lastDamageDirection);
        this.previousHull = this.hull;
        if(Time.time - this.lastDamageTime > this.shieldRechargeDelay)
        {
            this.shieldHP = Mathf.Clamp(this.shieldHP + Time.deltaTime / this.shieldRechargeTime, 0, this.maxShieldHP);
        }

        // this.shieldTransform.gameObject.SetActive(this.previousShield != this.shield);
        if (this.previousShield != this.shield)
        {
            if (this.shield == 0)
            {
                this.activeShieldAnim?.Stop();
                this.activeShieldAnim = Tween.LocalScale(this.shieldTransform, Vector3.one, Vector3.zero, 0.35f, 0f, Tween.EaseInBack, 
                    completeCallback: () =>
                    {
                        this.shieldFade = 0;
                        this.activeShieldAnim = null;
                    }
                );
            }
            else if (this.previousShield == 0)
            {
                this.activeShieldAnim?.Stop();
                this.activeShieldAnim = Tween.LocalScale(this.shieldTransform, Vector3.zero, Vector3.one, 0.35f, 0f, Tween.EaseSpring,
                    completeCallback: () =>
                    {
                        this.shieldFade = 3f;
                        this.activeShieldAnim = null;
                    }
                );
            }
            this.shieldFade = 1f;
            //if (this.activeShieldAnim == null)
            //{
            //    this.shieldTransform.localScale = Vector3.one * Mathf.Lerp(0.5f, 1f, this.shield);
            //}
        }
        this.shieldFade = Mathf.Max(0, this.shieldFade - Time.deltaTime);
        this.shieldRenderer.material.SetFloat("_Intensity", this.shieldFade);
        this.shieldRenderer.material.SetFloat("_Damage", 1 - this.shield);

        this.previousShield = this.shield;
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
        if (amount > 0)
        {
            this.lastDamageTime = Time.time;
        }

        float healthDamage = Mathf.Max(0, amount - this.shieldHP);
        this.shieldHP = Mathf.Clamp(this.shieldHP - amount, 0, this.maxShieldHP);
        this.hullHP = Mathf.Clamp(this.hullHP - healthDamage, 0, this.maxHullHP);

        if (healthDamage > 0)
        {
            this.lastDamageDirection = direction;
        }

        if (this.hull == 0)
        {
            this.gameLogic.LoseGame();
        }
    }

    public void AddHull(float amount)
    {
        this.hullHP = Mathf.Clamp(this.hullHP + amount, 0, this.maxHullHP);
    }
}
