using IngameDebugConsole;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class FuelTankComponent : MonoBehaviour, ISavable
{
    public float maxFuel = 1;
    
    [Tooltip("If true then this fuel tank can be refilled, if false then it will be discarded once used up.")]
    public bool refillable = true;

    [Saved]
    public float fuel = 1;

    public bool fullTank => this.fuel == this.maxFuel;
    public bool emptyTank => this.fuel == 0;

    public (float added, float remainder) GetAddFuelRemainder(float amount)
    {
        float added = Mathf.Min(this.maxFuel - this.fuel, amount);
        float remainder = amount - added;
        return (added, remainder);
    }
    
    public float AddFuelWithRemainder(float amount)
    {
        Assert.IsTrue(amount > 0, "Amount of fuel to add must be > 0");
        (float added, float remainder) = this.GetAddFuelRemainder(amount);
        this.fuel = Mathf.Min(this.fuel + added, this.maxFuel);
        return remainder;
    }

    public (float removed, float remainder) GetRemoveFuelRemainder(float amount)
    {
        float removed = Mathf.Min(this.fuel, amount);
        float remainder = amount - removed;
        return (removed, remainder);
    }
    
    public float RemoveFuelWithRemainder(float amount)
    {
        Assert.IsTrue(amount > 0, "Amount of fuel to remove must be > 0");
        (float removed, float remainder) = this.GetRemoveFuelRemainder(amount);
        this.fuel = Mathf.Max(this.fuel - removed, 0);
        return remainder;
    }
}