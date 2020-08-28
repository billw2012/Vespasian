using UnityEngine;
using UnityEngine.Assertions;

public class HealthComponent : MonoBehaviour
{
    public ParticleSystem damageDebris;
    public GameLogic gameLogic;

    [HideInInspector]
    public float health = 1;
    float previousHealth = 1;
    Vector3 lastDamageDirection;

    void Start()
    {
        Assert.IsNotNull(this.damageDebris);
        Assert.IsNotNull(this.gameLogic);

        this.damageDebris.SetEmissionEnabled(false);
    }


    void Update()
    {
        this.SetTakingDamage((this.previousHealth - this.health) / Time.deltaTime, this.lastDamageDirection);
        this.previousHealth = this.health;
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
        this.health = Mathf.Clamp(this.health - amount, 0, 1);
        if (amount > 0)
        {
            this.lastDamageDirection = direction;
        }

        if (this.health == 0)
        {
            this.gameLogic.LoseGame();
        }
    }
}
