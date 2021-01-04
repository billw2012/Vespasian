﻿using UnityEngine;

public class FuelScoop : MonoBehaviour, IUpgradeLogic
{
    public ParticleSystem particleEffect;
    public float refuelRate = 0.3f;

    private EngineController engineController;

    private void Awake()
    {
        this.engineController = this.GetComponentInParent<EngineController>();
    }

    private void Update()
    {
        var fuelSource = EffectSource.GetNearest<FuelSource>(this.transform);
        if (fuelSource != null)
        {
            fuelSource.Reveal();
        }
        //var engine = this.upgradeManager.GetComponentInChildren<FuelTankComponent>();
        if(fuelSource != null && this.engineController != null && this.engineController.canRefill)
        {
            float fuelIncrease = fuelSource.timeMultipler * Time.deltaTime * this.refuelRate;
            this.engineController.AddFuel(fuelIncrease); 
            
            // Set emission amount based on height ratio
            this.particleEffect.SetEmissionEnabled(true);
            float effectStrength = fuelSource.GetEffectStrengthNormalized(this.transform);
            this.particleEffect.SetEmissionRateOverTimeMultiplier(50.0f * Mathf.Clamp(0.05f + 1.0f * effectStrength, 0, 1));

            // Check if the source is on the left or right, flip particle effect if needed
            var offset = fuelSource.transform.position - this.transform.position;
            offset.z = 0;
            float distance = offset.magnitude;
            float scale = distance / 5.0f;
            var sourcePosInThisSpace = this.transform.InverseTransformDirection(offset);
            this.particleEffect.transform.localScale = sourcePosInThisSpace.x > 0
                ? scale * new Vector3(1, 1, 1)
                : scale * new Vector3(-1, 1, 1);
        }
        else
        {
            this.particleEffect.SetEmissionEnabled(false);
        }
    }

    #region IUpgradeLogic
    public UpgradeDef upgradeDef { get; private set; }
    public void Install(UpgradeDef upgradeDef) => this.upgradeDef = upgradeDef;
    public void Uninstall() {}
    public void TestFire() { }
    #endregion IUpgradeLogic
};
