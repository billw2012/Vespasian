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

    public SolarSystem jumpTarget { get; private set; }

    PlayerController player;

    UpgradeComponentProxy<WarpComponent> warpComponent;

    //// Start is called before the first frame update
    //void Awake()
    //{
    //    this.map = this.generator.Generate(this.bodySpecs);
    //}

    //void Start()
    //{
    //    _ = this.LoadRandomSystemAsync();
    //}

    void Start()
    {
        this.player = FindObjectOfType<PlayerController>();
        this.warpComponent = this.player.GetComponent<UpgradeManager>().GetProxy<WarpComponent>();
    }

    //void Update()
    //{
    //    if (this.player != null)
    //    {
    //        var playerDirection = this.player.transform.position;

    //        //this.jumpTarget = new Lazy<SolarSystem>(() => this.jumpTargets.Value
    //        //    .OrderBy(t => Vector2.Angle(t.position - this.currentSystem.position, playerDirection)).FirstOrDefault());
    //    }
    //    else
    //    {
    //        //this.jumpTarget = new Lazy<SolarSystem>(() => null);
    //    }
    //}

    public IEnumerable<SolarSystem> GetValidJumpTargets() => this.jumpTargets.Value;

    public bool TrySetJumpTarget(SolarSystem system)
    {
        if(this.jumpTargets.Value.Contains(system))
        {
            this.jumpTarget = system;
            return true;
        }
        else
        {
            return false;
        }
    }

    public async Task GenerateMapAsync() => this.map = await this.mapGenerator.GenerateAsync(this.bodySpecs);

    public bool CanJump() => this.jumpTarget != null && this.warpComponent.value.CanJump(this.currentSystem, this.jumpTarget);

    public float GetJumpFuelRequired() => this.warpComponent.value.GetJumpFuelRequired(this.currentSystem, this.jumpTarget);

    public async Task JumpAsyc()
    {
        Assert.IsNotNull(this.jumpTarget);

        await this.JumpAsync(this.jumpTarget);
    }

    public async Task LoadSystemAsync(SolarSystem target)
    {
        await target.LoadAsync(this.currentSystem, this.bodySpecs, this.gameObject);
        this.currentSystem = target;

        FindObjectOfType<SimManager>().Refresh();

        this.jumpTargets = new Lazy<List<SolarSystem>>(() =>
            this.map.GetConnected(this.currentSystem)
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
        var playerController = this.player.GetComponent<PlayerController>();
        playerController.enabled = false;
        playerController.SetAllowDamageAndCollision(false);

        // Send player into warp
        var playerSimMovement = this.player.GetComponent<SimMovement>();
        playerSimMovement.enabled = false;

        await this.warpComponent.value.Warp(this.currentSystem, target, this.LoadSystemAsync);

        // Re-enable player input
        playerController.enabled = true;

        // Safe to turn collision back on hopefully
        // TODO: make sure we never collide with something
        playerController.SetAllowDamageAndCollision(true);

        // Finally set the player velocity and re-enable simulation
        playerSimMovement.SimRefresh();
        playerSimMovement.enabled = true;

        this.uiManager.ShowPlayUI();
    }

    public async Task LoadRandomSystemAsync() => await this.JumpAsync(this.map.systems[Random.Range(0, this.map.systems.Count)]);
}
