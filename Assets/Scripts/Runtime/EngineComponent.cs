using IngameDebugConsole;
using System;
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
    
    public void UseFuelNoModifier(float amount)
    {
        this.fuel = Mathf.Clamp01(this.fuel - amount);
    }

    #region IUpdateLogic
    public UpgradeDef upgradeDef { get; private set; }
    public void Install(UpgradeDef upgradeDef) => this.upgradeDef = upgradeDef;
    public void Uninstall() { }
    public void TestFire() { }
    #endregion IUpdateLogic
    
    private static EngineComponent GetPlayerEngineComponent() =>
        FindObjectOfType<PlayerController>()?.GetComponentInChildren<EngineComponent>();
    
    [ConsoleMethod("player.ship.setfuel", "Set fuel of the players ship (0 - 1)")]
    public static void DebugSetPlayerShipFuel(float newFuel)
    {
        var playerEngineComponent = GetPlayerEngineComponent();
        if (playerEngineComponent != null)
        {
            playerEngineComponent.fuel = Mathf.Clamp(newFuel, 0, 1);
        }
    }
}