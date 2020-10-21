using System.Collections.Generic;
using UnityEngine;

public class EngineComponent : MonoBehaviour, IUpgradeLogic
{
    public float fuelUsageRate = 1;

    public float fuel { get; private set; } = 1;

    public bool canThrust => this.fuel > 0;

    //EngineController engineController;

    //void Start()
    //{
    //    this.engineController = this.GetComponentInParent<EngineController>();
    //}

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
    public object Save() => this.fuel;
    public void Load(object obj) => this.fuel = (float)obj;
    public void TestFire() { }
    #endregion IUpdateLogic
}