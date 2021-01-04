using IngameDebugConsole;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class FuelMainTankComponent : MonoBehaviour, IUpgradeLogic, ISavable, ISavableCustom
{
    [SerializeField]
    private FuelTankComponent tank = null;
    
    #region IUpgradeLogic
    public UpgradeDef upgradeDef { get; private set; }
    public void Install(UpgradeDef upgradeDef) => this.upgradeDef = upgradeDef;
    public void Uninstall() { }
    public void TestFire() { }
    #endregion IUpgradeLogic

    #region ISavableCustom
    public void Save(ISaver saver)
    {
        saver.SaveObject("maintank", tank);
    }

    public void Load(ILoader loader)
    {
        loader.LoadObject("maintank", tank);
    }
    #endregion ISavableCustom
}