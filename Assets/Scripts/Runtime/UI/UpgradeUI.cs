using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpgradeUI : MonoBehaviour
{
    public GameObject grid;
    public GameObject defaultUpgradeUIPrefab;

    UpgradeManager upgradeManager;

    void OnEnable()
    {
        this.upgradeManager = FindObjectOfType<UpgradeManager>();
        this.UpdateGrid();
    }

    void UpdateGrid()
    {
        foreach(Transform upgradeUI in this.grid.transform)
        {
            Destroy(upgradeUI.gameObject);
        }

        foreach(var upgradeDef in this.upgradeManager.fullUpgradeSet.upgradesDefs)
        {
            var upgradeUI = Instantiate(upgradeDef.shopUIPrefab == null? this.defaultUpgradeUIPrefab : upgradeDef.shopUIPrefab, this.grid.transform);
            var upgradeItemUI = upgradeUI.GetComponent<UpgradeItemUI>();
            upgradeItemUI.Init(this, upgradeDef);
        }
    }

    public void Install(UpgradeDef upgradeDef)
    {
        this.upgradeManager.Install(upgradeDef, testFire: true);
        this.UpdateGrid();
    }

    public bool CanInstall(UpgradeDef upgradeDef) => this.upgradeManager.CanInstall(upgradeDef);
    public bool IsInstalled(UpgradeDef upgradeDef) => this.upgradeManager.IsInstalled(upgradeDef);
}
