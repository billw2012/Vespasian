using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

public class FuelBoosterTankComponent : MonoBehaviour, IUpgradeLogic, ILevelUpgradeLogic, ISavable, ISavableCustom
{
    //public float maxFuel = 1;
    //[Tooltip("If true then this fuel tank can be refilled, if false then it will be discarded once used up.")]
    //public bool refillable = true;
    
    //[Saved]
    //public float fuel { get; set; } = 1;

    //public bool fullTank => this.fuel == this.maxFuel;

    private FuelTankComponent[] tanks = null;

    [Saved]
    private int level = 0;

    private void Awake()
    {
        this.tanks = this.GetComponentsInChildren<FuelTankComponent>();
        // // Start empty
        // foreach (var tank in this.tanks)
        // {
        //     tank.fuel = 0;
        // }
    }

    private void Update()
    {
        foreach (var activeTank in this.tanks.Where(t => t.enabled && t.emptyTank))
        {
            activeTank.enabled = false;
        }

        // If all tanks are empty then uninstall the booster upgrade
        if (this.tanks.All(t => t.emptyTank))
        {
            this.GetComponentInParent<UpgradeManager>().Uninstall(this.upgradeDef);
        }
    }

    #region IUpgradeLogic
    public UpgradeDef upgradeDef { get; private set; }
    public void Install(UpgradeDef upgradeDef)
    {
        this.upgradeDef = upgradeDef;
        this.LevelUp();
    }
    public void Uninstall() { }
    public void TestFire() { }
    #endregion IUpgradeLogic
    
    #region ILevelUpgradeLogic
    public void LevelUp()
    {
        Assert.IsTrue(this.Level < this.MaxLevel);
        this.tanks[this.level].fuel = this.tanks[this.level].maxFuel;
        this.tanks[this.level].enabled = true;
        this.level++;
    }

    public void LevelDown()
    {
        Assert.IsTrue(this.Level > 0);
        this.level--;
        this.tanks[this.level].fuel = 0;
        this.tanks[this.level].enabled = false;
    }
    
    public int MaxLevel => 3;
    
    public int Level => this.level;
    #endregion ILevelUpgradeLogic
    
    #region ISavableCustom
    public void Save(ISaver saver)
    {
        for (int i = 0; i < this.tanks.Length; i++)
        {
            saver.SaveObject($"boosttank{i}", this.tanks[i]);
        }
    }

    public void Load(ILoader loader)
    {
        for (int i = 0; i < this.tanks.Length; i++)
        {
            loader.LoadObject($"boosttank{i}", this.tanks[i]);
        }
    }
    #endregion ISavableCustom
}