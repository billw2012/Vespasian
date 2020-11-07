using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;


// TODO:
// Turn this into an abstract class, we will have different warp behavior
// Integrate WarpController into the abstract implementations of the WarpComponent? 
//   Might still be better to just have it separate as it is already encapsulated. Not sure...
public class WarpComponent : MonoBehaviour, IUpgradeLogic
{
    public float fuelUsageRate = 1;

    PlayerController player;
    UpgradeComponentProxy<EngineComponent> engine;
    
    void Start()
    {
        this.player = FindObjectOfType<PlayerController>();
        this.engine = this.player.GetComponent<UpgradeManager>().GetProxy<EngineComponent>();
    }

    public float JumpFuelRequired(SolarSystem from, SolarSystem to)
    {
        return (to.position - from.position).magnitude * this.fuelUsageRate;
    }

    public bool CanJump(SolarSystem from, SolarSystem to) => this.player.GetComponentInChildren<EngineComponent>().fuel >= this.JumpFuelRequired(from, to);

    public async Task Warp(SolarSystem from, SolarSystem to, Func<SolarSystem, Task> loadSystemCallback)
    {
        var playerWarpController = this.player.GetComponent<WarpController>();
        playerWarpController.enabled = true;

        // Set this before refreshing the sim so it is applied correctly
        Vector2 landingPosition;
        Vector2 landingVelocity;
        if (from != null)
        {
            var inTravelVec = (to.position - from.position).normalized;
            // var positionVec = Vector2.Perpendicular(inTravelVec) * (Random.value > 0.5f ? -1 : 1);

            landingPosition = inTravelVec * -Random.Range(30f, 50f) + Vector2.Perpendicular(inTravelVec) * Random.Range(-20f, +20f);
            landingVelocity = inTravelVec * Random.Range(0.5f, 1f);

            // Only use fuel when we are actually warping from somewhere of course
            this.engine.value.UseFuel(this.JumpFuelRequired(from, to));
        }
        else
        {
            landingPosition = Quaternion.Euler(0, 0, Random.Range(0f, 360f)) * Vector2.one * Random.Range(30f, 50f);
            landingVelocity = Quaternion.Euler(0, 0, Random.Range(0f, 360f)) * Vector2.one * Random.Range(0.5f, 1f);
        }

        await playerWarpController.EnterWarpAsync(landingVelocity.normalized, 50);

        await playerWarpController.TurnInWarpAsync(landingVelocity.normalized);

        await loadSystemCallback(to);

        await playerWarpController.ExitWarpAsync(landingPosition, landingVelocity.normalized, landingVelocity.magnitude);

        playerWarpController.enabled = false;

        this.player.GetComponent<SimMovement>().startVelocity = landingVelocity;
    }

    #region IUpdateLogic
    public UpgradeDef upgradeDef { get; private set; }
    public void Install(UpgradeDef upgradeDef) => this.upgradeDef = upgradeDef;
    public void Uninstall() { }
    public object Save() => null;
    public void Load(object obj) { }
    public void TestFire() { }
    #endregion IUpdateLogic
}