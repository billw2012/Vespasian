using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

public class MapComponent : MonoBehaviour, ISavable, IPreSave, ISavableCustom, IPostLoadAsync
{
    public GUILayerManager uiManager;
    public BodySpecs bodySpecs;
    public MapGenerator mapGenerator;

    [NonSerialized, Saved] public Map map;

    [NonSerialized]
    public SolarSystem currentSystem;

    //public delegate void MapGenerated();
    public event Action MapGenerated;
    
    private Lazy<List<SolarSystem>> jumpTargets = new Lazy<List<SolarSystem>>(() => new List<SolarSystem>());

    public SolarSystem jumpTarget { get; private set; }

    private PlayerController player;

    private UpgradeComponentProxy<WarpComponent> warpComponent;

    //// Start is called before the first frame update
    //void Awake()
    //{
    //    this.map = this.generator.Generate(this.bodySpecs);
    //}

    //void Start()
    //{
    //    _ = this.LoadRandomSystemAsync();
    //}

    private void Awake()
    {
        this.player = FindObjectOfType<PlayerController>();
        this.warpComponent = this.player.GetComponent<UpgradeManager>().GetProxy<WarpComponent>();

        var saveSystem = FindObjectOfType<SaveSystem>();
        if (saveSystem != null)
        {
            saveSystem.RegisterForSaving(this);
        }
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

    public async Task GenerateMapAsync()
    {
        this.map = await this.mapGenerator.GenerateAsync(this.bodySpecs);
        this.MapGenerated?.Invoke();
    }

    public bool CanJump() => this.jumpTarget != null && this.warpComponent.value.CanJump(this.currentSystem, this.jumpTarget);

    public float GetJumpFuelRequired() => this.warpComponent.value.GetJumpFuelRequired(this.currentSystem, this.jumpTarget);

    public async Task JumpAsyc()
    {
        Assert.IsNotNull(this.jumpTarget);

        await this.JumpAsync(this.jumpTarget);
    }

    public async Task LoadSystemAsync(SolarSystem target)
    {
        await this.LoadSystemAsync(this.currentSystem, target);
    }

    public async Task JumpAsync(SolarSystem target, Func<Vector2?> landingPositionCallback = default)
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
        var playerSimMovement = this.player.GetComponent<SimMovement>();
        playerSimMovement.enabled = false;

        await this.warpComponent.value.WarpAsync(this.currentSystem, target, this.LoadSystemAsync, landingPositionCallback);

        // Re-enable player input
        playerController.enabled = true;

        // Safe to turn collision back on hopefully
        // TODO: make sure we never collide with something
        playerController.SetAllowDamageAndCollision(true);

        // Finally set the player velocity and re-enable simulation
        playerSimMovement.SimRefresh();
        playerSimMovement.enabled = true;

        this.uiManager.ShowUI();
    }

    public async Task LoadStartingSystemAsync()
    {
        var faction = FindObjectOfType<Faction>();
        if(faction && faction.stations.Any())
        {
            var randomFactionStation = faction.stations.SelectRandom();
            StationLogic stationToDockAt = null;
            await this.JumpAsync(this.map.systems[randomFactionStation.systemId], () =>
            {
                // If there is a station in the system then land near it
                stationToDockAt = FindObjectsOfType<StationLogic>().SelectRandom();
                return stationToDockAt?.transform.position;
            });
            if (stationToDockAt != null)
            {
                this.player.GetComponent<DockActive>().DockAt(stationToDockAt.GetComponentsInChildren<DockPassive>().SelectRandom());
            }
        }
        else
        {
            await this.LoadRandomSystemAsync();
        }
    }
    
    public async Task LoadRandomSystemAsync() => await this.JumpAsync(this.map.systems.SelectRandom());

    private async Task LoadSystemAsync(SolarSystem from, SolarSystem to)
    {
        await to.LoadAsync(from, this.bodySpecs, this.gameObject);
        this.currentSystem = to;

        FindObjectOfType<Simulation>().Refresh();

        this.jumpTargets = new Lazy<List<SolarSystem>>(() =>
            this.map.GetConnected(this.currentSystem)
                .Select(t => t.system)
                .ToList()
            );
    }

    public int GetDataCreditValue(BodyRef bodyRef, DataMask data)
    {
        var body = this.map.GetBody(bodyRef);
        int baseValue = body.GetDataCreditValue(data);
        float multiplier = this.bodySpecs.GetSpecById(body.specId).dataValueMultiplier;
        return Mathf.CeilToInt(baseValue * multiplier);
    }

    #region ISavableCustom
    public void Save(ISaver saver)
    {
        //serializer.SaveObject("map", this.map);
        saver.SaveValue("currentIdx", this.map.systems.IndexOf(this.currentSystem));

        var dockActive = this.player.GetComponentInChildren<DockActive>();//.Save(saver);
        saver.SaveValue("docked", dockActive.docked);
        if (dockActive.docked)
        {
            // There should be a BodyGenerator in the parent chain from a passive docking port, representing the
            // station we are docked to.
            var dockTarget = dockActive.passiveDockingPort.GetComponentInParent<BodyGenerator>();
            saver.SaveValue("dockTarget.BodyRef", dockTarget.BodyRef);
            int dockingPortIndex = Array.IndexOf(
                dockTarget.GetComponentsInChildren<DockPassive>().ToArray(),
                dockActive.passiveDockingPort);
            saver.SaveValue("dockTarget.Index", dockingPortIndex);
        }
    }

    private BodyRef dockTargetBodyRef;
    private int dockTargetIndex;
    
    public void Load(ILoader loader)
    {
        //deserializer.LoadObject("map", this.map);
        this.currentSystem = this.map.systems[loader.LoadValue<int>("currentIdx")];
        
        if (loader.LoadValue<bool>("docked"))
        {
            this.dockTargetBodyRef = loader.LoadValue<BodyRef>("dockTarget.BodyRef");
            this.dockTargetIndex = loader.LoadValue<int>("dockTarget.Index");
        }
        else
        {
            this.dockTargetBodyRef = null;
        }
    }
    #endregion

    #region IPostLoadAsync
    public async Task OnPostLoadAsync()
    {
        // We need to load into the current system after game save load is complete
        await this.LoadSystemAsync(null, this.currentSystem);
        
        if (this.dockTargetBodyRef != null)
        {
            var dockTarget = FindObjectsOfType<BodyGenerator>().FirstOrDefault(b => b.BodyRef == this.dockTargetBodyRef);
            Assert.IsNotNull(dockTarget, $"Couldn't find docking target with BodyRef {this.dockTargetBodyRef}");
            var dockingPortTargets =
                dockTarget.GetComponentsInChildren<DockPassive>().ToArray();
            this.player.GetComponent<DockActive>().DockAt(dockingPortTargets[this.dockTargetIndex]);
        }
    }
    #endregion

    public void PreSave()
    {
        this.currentSystem?.SaveAll();
    }
}
