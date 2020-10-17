using UnityEngine;
using System.Collections.Generic;
using Object = UnityEngine.Object;
using System.Linq;

public class UpgradeManager : MonoBehaviour
{
    public UpgradeSet fullUpgradeSet;
    public UpgradeSet initialUpgrades;
    public Transform upgradeRoot;

    void Start()
    {
        this.InstallInitialUpgrades();
    }

    public void InstallInitialUpgrades()
    {
        foreach(var upgradeDef in this.initialUpgrades.upgradesDefs)
        {
            this.Install(upgradeDef);
        }
    }

    public IUpgradeLogic[] GetInstalledUpgrades() => this.upgradeRoot.GetComponentsInChildren<IUpgradeLogic>();

    public bool Allowed(UpgradeDef upgradeDef) => upgradeDef.requires.All(u => this.HasUpgrade(u.name));

    public IUpgradeLogic FindUpgrade(string name) => this.GetInstalledUpgrades().FirstOrDefault(u => u.upgradeDef.name == name);

    public bool HasUpgrade(string name) => this.FindUpgrade(name) != null;

    public IUpgradeLogic Install(UpgradeDef upgrade)
    {
        var obj = Object.Instantiate(upgrade.shipPartPrefab, this.upgradeRoot);
        obj.name = upgrade.name;
        var upgradeLogic = obj.GetComponent<IUpgradeLogic>();
        upgradeLogic.Install(upgrade);
        return upgradeLogic;
    }

    public void LoadUpgrades(List<(string name, object data)> saveData)
    {
        this.UninstallAllUpgrades();
        foreach (var (name, data) in saveData)
        {
            var upgradeDef = this.fullUpgradeSet.GetUpgradeDef(name);
            var upgradeLogic = this.Install(upgradeDef);
            upgradeLogic.Load(data);
        }
    }

    public List<(string name, object data)> SaveUpgrades() => this.GetInstalledUpgrades().Select(u => (u.upgradeDef.name, u.Save())).ToList();

    public void UninstallAllUpgrades()
    {
        foreach (var upgrade in this.GetInstalledUpgrades())
        {
            Uninstall(upgrade);
        }
    }

    public void Uninstall(IUpgradeLogic upgradeLogic)
    {
        upgradeLogic.Uninstall();
        var obj = (upgradeLogic as MonoBehaviour).gameObject;
        // obj.transform.SetParent(null);
        Debug.Log($"Uninstalled upgrade {upgradeLogic.upgradeDef.name} from ship {this.gameObject.name}");

        Destroy(obj);
    }

    public void Uninstall(UpgradeDef upgrade)
    {
        var upgradeLogic = this.FindUpgrade(upgrade.name);
        if (upgradeLogic != null)
        {
            this.Uninstall(upgradeLogic);
        }
        else
        {
            Debug.LogWarning($"Could not uninstall upgrade {upgrade.name} as it didn't exist on ship {this.gameObject.name}");
        }
    }
}

public interface IUpgradeLogic
{
    UpgradeDef upgradeDef { get; }
    void Install(UpgradeDef upgradeDef);
    void Uninstall();
    object Save();
    void Load(object obj);
}