using System;
using System.Collections.Generic;
using System.Linq;
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

    [Tooltip("How quickly efficiency drops off relative to divergence from the perfect exit trajectory"), Range(0.2f, 3f)]
    public float accuracyRequirementFalloff = 0.38f;
    [Tooltip("Multiplier for total effect of the accuracy"), Range(0f, 1.25f)]
    public float accuracyRequirementScaling = 0.79f;

    private const float FuelUsageBaseRate = 5;

    private PlayerController player;
    private SimMovement playerMovement;
    private UpgradeComponentProxy<EngineComponent> engine;

    private void Awake()
    {
        this.player = FindObjectOfType<PlayerController>();
        this.playerMovement = this.player.GetComponent<SimMovement>();
        this.engine = this.player.GetComponent<UpgradeManager>().GetProxy<EngineComponent>();
    }

    public float GetJumpFuelRequired(SolarSystem from, SolarSystem to)
    {
        if(from == null || to == null)
        {
            return 0;
        }
        var warpRouteVector = to.position - from.position;
        float baseCost = warpRouteVector.magnitude * FuelUsageBaseRate;

        // var playerAngle = ;
        // See https://www.desmos.com/calculator/50kd2kcivr
        float FuelEfficiency()
        {
            float divergence = Vector2.Angle(this.playerMovement.velocity.normalized, warpRouteVector) / 180f;
            return Mathf.Pow(divergence, this.accuracyRequirementFalloff) * this.accuracyRequirementScaling;
        }
        return baseCost * FuelEfficiency() * this.fuelUsageRate;
    }

    public bool CanJump(SolarSystem from, SolarSystem to) => this.player.GetComponentInChildren<EngineComponent>().fuel >= this.GetJumpFuelRequired(from, to);

    public async Task WarpAsync(SolarSystem from, SolarSystem to, Func<SolarSystem, Task> loadSystemCallback, Func<Vector2?> landingPositionCallback = default)
    {
        var playerWarpController = this.player.GetComponent<WarpController>();
        playerWarpController.enabled = true;

        // Set this before refreshing the sim so it is applied correctly
        
        var exitDirection = from != null? (to.position - from.position).normalized : (Vector2) this.player.transform.up;
        
        if (from != null)
        {
            exitDirection = (to.position - from.position).normalized;
            // var positionVec = Vector2.Perpendicular(inTravelVec) * (Random.value > 0.5f ? -1 : 1);

            // Only use fuel when we are actually warping from somewhere of course
            this.engine.value.UseFuel(this.GetJumpFuelRequired(from, to));
        }

        await playerWarpController.EnterWarpAsync(exitDirection, 50);

        await loadSystemCallback(to);

        Vector2 GetDefaultLandingPosition()
        {
            var entryDirection = Quaternion.Euler(0, 0, Random.Range(0f, 360f)) * Vector2.one;
            float entryDistance = Mathf.Max(50f, Random.Range(0f, 0.5f) * to.size + to.main.radius * 10f);
            return Vector2.Perpendicular(entryDirection) * Mathf.Sign(Random.Range(-1, +1)) * entryDistance;
        }

        var landingPosition = landingPositionCallback?.Invoke() ?? GetDefaultLandingPosition();
        
        // var station = FindObjectsOfType<StationLogic>().SelectRandom();
        // if (station != null)
        // {
        //     // If there is a station in the system then land near it
        //     landingPosition = station.transform.position;
        // }
        // else
        // {
        //     var entryDirection = Quaternion.Euler(0, 0, Random.Range(0f, 360f)) * Vector2.one;
        //     float entryDistance = Mathf.Max(50f, Random.Range(0f, 0.5f) * to.size + to.main.radius * 10f);
        //     landingPosition = Vector2.Perpendicular(entryDirection) * Mathf.Sign(Random.Range(-1, +1)) * entryDistance;
        // }
        
        float speedAtPeriapsis = OrbitalUtils.SpeedAtPeriapsis(landingPosition.magnitude,
            landingPosition.magnitude,
            to.main.mass,
            this.playerMovement.constants.GravitationalConstant);

        var landingVelocity = Vector2.Perpendicular(landingPosition.normalized) * speedAtPeriapsis;

        await playerWarpController.TurnInWarpAsync(landingVelocity.normalized);

        await playerWarpController.ExitWarpAsync(landingPosition, landingVelocity.magnitude);

        playerWarpController.enabled = false;

        this.player.GetComponent<SimMovement>().startVelocity = landingVelocity;
    }

    #region IUpdateLogic
    public UpgradeDef upgradeDef { get; private set; }
    public void Install(UpgradeDef upgradeDef) => this.upgradeDef = upgradeDef;
    public void Uninstall() { }
    public void TestFire() { }
    #endregion IUpdateLogic
}