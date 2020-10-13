using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;
using Random = UnityEngine.Random;

public class MapComponent : MonoBehaviour
{
    public GUILayerManager uiManager;
    public BodySpecs bodySpecs;
    public MapGenerator mapGenerator;

    [NonSerialized]
    public Map map;

    [NonSerialized]
    public SolarSystem currentSystem;

    Lazy<List<SolarSystem>> jumpTargets = new Lazy<List<SolarSystem>>(() => new List<SolarSystem>());

    Lazy<SolarSystem> jumpTarget = new Lazy<SolarSystem>(() => null);

    PlayerController player => FindObjectOfType<PlayerController>();

    //// Start is called before the first frame update
    //void Awake()
    //{
    //    this.map = this.generator.Generate(this.bodySpecs);
    //}

    //void Start()
    //{
    //    _ = this.LoadRandomSystemAsync();
    //}

    void Update()
    {
        if (this.player != null)
        {
            var playerDirection = this.player.transform.position;

            this.jumpTarget = new Lazy<SolarSystem>(() => this.jumpTargets.Value
                .OrderBy(t => Vector2.Angle(t.position - this.currentSystem.position, playerDirection)).FirstOrDefault());
        }
        else
        {
            this.jumpTarget = new Lazy<SolarSystem>(() => null);
        }
    }

    public async Task GenerateMapAsync() => this.map = await this.mapGenerator.GenerateAsync(this.bodySpecs);

    public SolarSystem GetJumpTarget() => this.jumpTarget.Value;

    public bool CanJump() => this.GetJumpTarget() != null;

    public async Task JumpAsyc()
    {
        Assert.IsNotNull(this.GetJumpTarget());

        await this.JumpAsync(this.GetJumpTarget());
    }

    public async Task LoadSystemAsync(SolarSystem target)
    {
        this.currentSystem = target;
        await this.currentSystem.LoadAsync(this.bodySpecs, this.gameObject);

        FindObjectOfType<SimManager>().Refresh();

        this.jumpTargets = new Lazy<List<SolarSystem>>(() =>
            this.map.GetJumpTargets(this.currentSystem)
                .Select(t => t.system)
                .ToList()
            );
    }

    public async Task JumpAsync(SolarSystem target)
    {
        if(this.currentSystem == target)
        {
            Debug.Log("Jumped to current system");
            return;
        }

        this.uiManager.HideUI();

        // Player enter warp

        // Disable player input
        this.player.GetComponent<PlayerController>().enabled = false;

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

        await this.LoadSystemAsync(target);

        await playerWarpController.ExitWarpAsync(landingPosition, landingVelocity.normalized, landingVelocity.magnitude);

        // Safe to turn collision back on hopefully
        foreach (var collider in this.player.GetComponentsInChildren<Collider>())
        {
            collider.enabled = true;
        }

        playerWarpController.enabled = false;

        playerSimMovement.SetVelocity(landingVelocity);
        playerSimMovement.enabled = true;

        // Re-enable player input
        this.player.GetComponent<PlayerController>().enabled = true;

        this.uiManager.ShowPlayUI();
    }

    public async Task LoadRandomSystemAsync() => await this.JumpAsync(this.map.systems[Random.Range(0, this.map.systems.Count)]);
}
