using UnityEngine;
using System.Collections.Generic;
using Object = UnityEngine.Object;
using System.Linq;
using System;

public interface IUpgradeComponentProxy
{
    void Invalidate();
}

/// <summary>
/// Used to accelerate lookup of components inside upgrades, avoiding a 
/// hierarchical Component search on every access (uses lazy evaluation).
/// </summary>
/// <typeparam name="T"></typeparam>
public class UpgradeComponentProxy<T> : IUpgradeComponentProxy where T: MonoBehaviour, IUpgradeLogic
{
    public T value => this.component.Value;

    private readonly UpgradeManager owner;
    private Lazy<T> component = new Lazy<T>(() => default(T));

    public UpgradeComponentProxy(UpgradeManager owner)
    {
        this.owner = owner;
        owner.RegisterProxy(this);
        this.Invalidate();
    }

    public void Invalidate()
    {
        this.component = new Lazy<T>(() => this.owner.upgradeRoot.GetComponentInChildren<T>());
    }
}

/// <summary>
/// Manages the set of upgrades on a ship, e.g. install, uninstall, save and load.
/// </summary>
public class UpgradeManager : MonoBehaviour, ISavable, ISavableCustom
{
    public UpgradeSet fullUpgradeSet;
    public UpgradeSet initialUpgrades;
    public Transform upgradeRoot;

    private readonly List<IUpgradeComponentProxy> proxies = new List<IUpgradeComponentProxy>();

    private void Start()
    {
        FindObjectOfType<SaveSystem>().RegisterForSaving(this);

        this.InstallInitialUpgrades();
    }

    public UpgradeComponentProxy<T> GetProxy<T>() where T : MonoBehaviour, IUpgradeLogic
    {
        return new UpgradeComponentProxy<T>(this);
    }

    public void RegisterProxy(IUpgradeComponentProxy proxy)
    {
        this.proxies.Add(proxy);
    }

    private void InvalidateProxies()
    {
        foreach(var proxy in this.proxies)
        {
            proxy.Invalidate();
        }
    }

    public void InstallInitialUpgrades()
    {
        foreach(var upgradeDef in this.initialUpgrades.upgradesDefs)
        {
            this.Install(upgradeDef);
        }
    }

    public IUpgradeLogic[] GetInstalledUpgrades() => this.upgradeRoot.GetComponentsInChildren<IUpgradeLogic>();

    public bool CanInstall(UpgradeDef upgradeDef) => upgradeDef.requires.All(u => this.IsInstalled(u.name)) && !this.IsInstalled(upgradeDef.name);

    public IUpgradeLogic FindInstalledUpgrade(string name) => this.GetInstalledUpgrades().FirstOrDefault(u => u.upgradeDef.name == name);

    public bool IsInstalled(string name) => this.FindInstalledUpgrade(name) != null;
    public bool IsInstalled(UpgradeDef upgradeDef) => this.FindInstalledUpgrade(upgradeDef.name) != null;

    public IUpgradeLogic Install(UpgradeDef upgradeDef, bool testFire = false)
    {
        this.InvalidateProxies();

        foreach (var replaced in upgradeDef.replaces.Where(u => this.IsInstalled(u.name)))
        {
            this.Uninstall(replaced);
        }
        var obj = Object.Instantiate(upgradeDef.shipPartPrefab, this.upgradeRoot);
        obj.name = upgradeDef.name;
        var upgradeLogic = obj.GetComponent<IUpgradeLogic>();
        upgradeLogic.Install(upgradeDef);
        if(testFire)
        {
            upgradeLogic.TestFire();
        }

        Debug.Log($"Installed upgrade {upgradeDef.name} on ship {this.gameObject.name}");


        return upgradeLogic;
    }

    public void Uninstall(IUpgradeLogic upgradeLogic)
    {
        this.InvalidateProxies();

        upgradeLogic.Uninstall();
        var obj = (upgradeLogic as MonoBehaviour).gameObject;
        // obj.transform.SetParent(null);
        Debug.Log($"Uninstalled upgrade {upgradeLogic.upgradeDef.name} from ship {this.gameObject.name}");
        obj.SetActive(false);
        obj.transform.SetParent(null);
        Destroy(obj);
    }

    public void Uninstall(UpgradeDef upgrade)
    {
        var upgradeLogic = this.FindInstalledUpgrade(upgrade.name);
        if (upgradeLogic != null)
        {
            this.Uninstall(upgradeLogic);
        }
        else
        {
            Debug.LogWarning($"Could not uninstall upgrade {upgrade.name} as it didn't exist on ship {this.gameObject.name}");
        }
    }

    public void UninstallAllUpgrades()
    {
        foreach (var upgrade in this.GetInstalledUpgrades())
        {
            this.Uninstall(upgrade);
        }
    }

    #region ISavableCustom
    [RegisterSavableType(typeof(List<string>))]
    public void Save(ISaver saver)
    {
        var upgrades = this.GetInstalledUpgrades();
        // save a list of the upgrade names
        saver.SaveValue("UpgradeList", upgrades.Select(u => u.upgradeDef.name).ToList());
        // save any data they want to save
        foreach (var upgrade in upgrades.OfType<ISavable>())
        {
            saver.SaveObject("Upgrade." + (upgrade as IUpgradeLogic).upgradeDef.name, upgrade);
        }
    }

    public void Load(ILoader loader)
    {
        this.UninstallAllUpgrades();

        var upgradeNames = loader.LoadValue<List<string>>("UpgradeList");
        foreach(string upgradeName in upgradeNames)
        {
            var upgradeDef = this.fullUpgradeSet.GetUpgradeDef(upgradeName);
            var upgradeLogic = this.Install(upgradeDef);
            if(upgradeLogic is ISavable)
            {
                loader.LoadObject("Upgrade." + upgradeLogic.upgradeDef.name, upgradeLogic as ISavable);
            }
        }
    }
    #endregion ISavableCustom
}

/// <summary>
/// Interface to be implemented by all ship upgrades.
/// </summary>
public interface IUpgradeLogic
{
    /// <summary>
    /// Returns the UpgradeDef this upgrade is associated with
    /// </summary>
    UpgradeDef upgradeDef { get; }

    /// <summary>
    /// Perform any extra installation of the upgrade.
    /// The instancing of the prefab etc. will be performed by the UpgradeManager
    /// itself, this function only needs to implement any special requirements.
    /// It should also set the upgradeDef property using the passed in parameter.
    /// This function is called on first time install, and on loading a save game, 
    /// before Load() is called.
    /// </summary>
    /// <param name="upgradeDef">The UpgradeDef to install</param>
    void Install(UpgradeDef upgradeDef);

    /// <summary>
    /// Used to show some indication to the player that the Upgrade is present or installed.
    /// e.g. Show shield visual, test fire boosters, etc.
    /// </summary>
    void TestFire();

    /// <summary>
    /// Implement any custom behavior for uninstalling an upgrade here.
    /// </summary>
    void Uninstall();
}