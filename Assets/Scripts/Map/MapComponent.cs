using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;
using Random = UnityEngine.Random;

public class MapComponent : MonoBehaviour
{
    public BodySpecs bodySpecs;
    public MapGenerator generator;

    public Map map;

    public SolarSystem currentSystem;

    Lazy<List<SolarSystem>> jumpTargets = new Lazy<List<SolarSystem>>(() => new List<SolarSystem>());

    PlayerController player => FindObjectOfType<PlayerController>();

    // Start is called before the first frame update
    void Awake()
    {
        this.map = this.generator.Generate(this.bodySpecs);
    }

    void Start()
    {
        this.LoadRandomSystem();
    }

    public SolarSystem GetJumpTarget()
    {
        if(this.player != null)
        {
            var playerDirection = this.player.transform.position;

            return this.jumpTargets.Value
                .OrderBy(t => Vector2.Angle(t.position - this.currentSystem.position, playerDirection)).FirstOrDefault();
        }
        else
        {
            return null;
        }
    }

    public bool CanJump()
    {
        return this.GetJumpTarget() != null;
    }

    public async Task JumpAsyc()
    {
        Assert.IsNotNull(this.GetJumpTarget());

        await this.Jump(this.GetJumpTarget());
    }

    public async Task Jump(SolarSystem target)
    {
        if(this.currentSystem == target)
        {
            Debug.Log("Jumped to current system");
            return;
        }

        // Player enter warp

        // Disable player input
        this.player.GetComponent<PlayerController>().enabled = false;

        var travelVec = this.currentSystem != null
            ? (target.position - this.currentSystem.position).normalized
            : (Vector2)(Quaternion.Euler(0, 0, Random.Range(0f, 360f)) * Vector3.one)
            ;

        // Send player into warp
        var playerSimMovement = this.player.GetComponent<SimMovement>();
        var playerWarpController = this.player.GetComponent<WarpController>();
        playerSimMovement.enabled = false;
        playerWarpController.enabled = true;

        foreach (var collider in this.player.GetComponentsInChildren<Collider>())
        {
            collider.enabled = false;
        }

        // Set this before refreshing the sim so it is applied correctly

        Vector2 landingPosition;
        Vector2 landingVelocity;
        if (this.currentSystem != null)
        {
            var inTravelVec = (target.position - this.currentSystem.position).normalized;
            // var positionVec = Vector2.Perpendicular(inTravelVec) * (Random.value > 0.5f ? -1 : 1);

            landingPosition = inTravelVec * -Random.Range(30f, 50f) + Vector2.Perpendicular(inTravelVec) * Random.Range(-20f, +20f);
            landingVelocity = inTravelVec * Random.Range(0.5f, 1f);
        }
        else
        {
            landingPosition = Quaternion.Euler(0, 0, Random.Range(0f, 360f)) * Vector2.one * Random.Range(30f, 50f);
            landingVelocity = Quaternion.Euler(0, 0, Random.Range(0f, 360f)) * Vector2.one * Random.Range(0.5f, 1f);
        }

        await playerWarpController.EnterWarpAsync(landingVelocity.normalized, 50);

        await playerWarpController.TurnInWarpAsync(landingVelocity.normalized);

        // Destroy old system, update player position and create new one
        this.currentSystem = target;
        this.currentSystem.Load(this.gameObject);

        this.jumpTargets = new Lazy<List<SolarSystem>>(() => 
            this.map.GetJumpTargets(this.currentSystem)
                .Select(t => t.system)
                .ToList()
            );

        await playerWarpController.ExitWarpAsync(landingPosition, landingVelocity.normalized, landingVelocity.magnitude);

        // Safe to turn collision back on hopefully
        foreach (var collider in this.player.GetComponentsInChildren<Collider>())
        {
            collider.enabled = true;
        }

        playerWarpController.enabled = false;

        playerSimMovement.startVelocity = landingVelocity;
        playerSimMovement.enabled = true;

        FindObjectOfType<SimManager>().Refresh();

        // Re-enable player input
        this.player.GetComponent<PlayerController>().enabled = true;
    }

    public void LoadRandomSystem()
    {
        _ = this.Jump(this.map.systems[Random.Range(0, this.map.systems.Count)]);
    }
}
