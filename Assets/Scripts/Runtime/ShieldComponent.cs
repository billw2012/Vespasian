using Pixelplacement;
using Pixelplacement.TweenSystem;
using UnityEngine;

public class ShieldComponent : MonoBehaviour, IUpgradeLogic
{
    [Tooltip("Time to fully recharge shield"), Range(1, 30)]
    public float shieldRechargeTime = 10f;
    [Tooltip("Time without damage taken before shield will start recharging"), Range(0, 30)]
    public float shieldRechargeDelay = 5f;

    [Tooltip("Shield strength"), Range(0, 3)]
    public float maxShieldHP = 0.5f;

    public float shield => this.shieldHP / this.maxShieldHP;

    public Transform shieldTransform;
    public MeshRenderer shieldRenderer;

    float shieldHP;
    float previousShield = 1;
    float lastDamageTime = float.MinValue;

    TweenBase activeShieldAnim;
    float shieldFade = 0f;

    void Start()
    {
        this.shieldHP = this.maxShieldHP;
    }

    void Update()
    {
        if (Time.time - this.lastDamageTime > this.shieldRechargeDelay)
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

    public float AddDamage(float amount)
    {
        if (amount > 0)
        {
            this.lastDamageTime = Time.time;
        }

        float healthDamage = Mathf.Max(0, amount - this.shieldHP);
        this.shieldHP = Mathf.Clamp(this.shieldHP - amount, 0, this.maxShieldHP);

        return healthDamage;
    }

    #region IUpgradeLogic
    public UpgradeDef upgradeDef { get; private set; }

    public void Install(UpgradeDef upgradeDef)
    {
        this.upgradeDef = upgradeDef;
    }

    public void Uninstall() { }

    public object Save()
    {
        return (this.shieldHP, this.previousShield, this.lastDamageTime);
    }

    public void Load(object obj)
    {
        (this.shieldHP, this.previousShield, this.lastDamageTime) = ((float, float, float))obj;
    }
    #endregion
}
