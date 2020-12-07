using System.Collections.Generic;
using UnityEngine;

public class EngineComponent : MonoBehaviour, IUpgradeLogic, ISavable
{
    public float fuelUsageRate = 1;

    [Saved]
    public float fuel { get; private set; } = 1;

    public bool fullTank => this.fuel == 1;

    public bool canThrust => this.fuel > 0;

    public void AddFuel(float amount)
    {
        this.fuel = Mathf.Clamp01(this.fuel + amount);
    }

    public void UseFuel(float amount)
    {
        this.fuel = Mathf.Clamp01(this.fuel - amount * this.fuelUsageRate);
    }

    #region IUpdateLogic
    public UpgradeDef upgradeDef { get; private set; }
    public void Install(UpgradeDef upgradeDef) => this.upgradeDef = upgradeDef;
    public void Uninstall() { }
    public void TestFire() { }
    #endregion IUpdateLogic
}