using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Events;

public class MapComponent : MonoBehaviour, ISavable, IPreSave, ISavableCustom, IPostLoadAsync
{
    [SerializeField]
    private GUILayerManager uiManager = null;
    public BodySpecs bodySpecs = null;
    [SerializeField]
    private MapGenerator mapGenerator = null;

    [Saved]
    public Map map { get; private set; }

    /// <summary>
    /// System the player is currently in
    /// </summary>
    public SolarSystem currentSystem { get; private set; }

    public GameObject primary => this.currentSystem.primary;

    /// <summary>
    /// System selected in the UI
    /// </summary>
    public SolarSystem selectedSystem { get; set; }

    //public delegate void MapGenerated();
    public UnityEvent mapGenerated;
    
    public SolarSystem jumpTarget { get; private set; }

    private PlayerController player;
    private DockActive playerDockActive;

    private UpgradeComponentProxy<WarpComponent> warpComponent;

    private Lazy<List<SolarSystem>> jumpTargets = new Lazy<List<SolarSystem>>(() => new List<SolarSystem>());

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
        this.player = ComponentCache.FindObjectOfType<PlayerController>();
        this.playerDockActive = this.player?.GetComponentInChildren<DockActive>();
        this.warpComponent = this.player?.GetComponent<UpgradeManager>().GetProxy<WarpComponent>();

        ComponentCache.FindObjectOfType<SaveSystem>()?.RegisterForSaving(this);
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
        this.mapGenerated?.Invoke();
    }

    public void GenerateMap()
    {
        _ = this.GenerateMapAsync();
    }

    /// <summary>
    /// Can jump if:
    /// <list type="bullet">
    /// <item><description>a valid target is selected</description></item>  
    /// <item><description>we aren't docked</description></item>
    /// <item><description>the installed <see cref="WarpComponent"/> upgrade says we can (see the <c>CanJump</c> function)</description></item>
    /// </list> 
    /// </summary>
    /// <returns>True if warp is currently possible</returns>
    public bool CanJump() => this.jumpTarget != null && !this.playerDockActive.docked && this.warpComponent.value.CanJump(this.currentSystem, this.jumpTarget);

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
        this.player.GetComponent<ControllerBase>()?.SetControlled(false);
        // var controller = this.player.GetComponent<PlayerController>();
        // controller.enabled = false;
        // //var health = this.GetComponent<HealthComponent>();
        // health?.SetAllowDamageAndCollision(false);
        // var simMovement = this.player.GetComponent<SimMovement>();
        // simMovement.enabled = false;

        await this.warpComponent.value.WarpAsync(this.currentSystem, target, this.LoadSystemAsync, landingPositionCallback);

        // // Re-enable control
        // controller.enabled = true;
        //
        // // Safe to turn collision back on hopefully
        // health?.SetAllowDamageAndCollision(false);
        // controller.SetAllowDamageAndCollision(true);
        //
        // // Finally set the player velocity and re-enable simulation
        // // playerSimMovement.SimRefresh(ComponentCache.FindObjectOfType<Simulation>());
        // simMovement.enabled = true;
        this.player.GetComponent<ControllerBase>()?.SetControlled(true);

        this.uiManager.ShowUI();
    }

    public async Task LoadStartingSystemAsync()
    {
        var factionExansion = ComponentCache.FindObjectOfType<FactionExpansion>();
        if(factionExansion != null && factionExansion.stations.Any())
        {
            var randomFactionStation = factionExansion.stations.SelectRandom();
            StationLogic stationToDockAt = null;
            await this.JumpAsync(this.map.systems[randomFactionStation.systemId], () =>
            {
                // If there is a station in the system then land near it
                stationToDockAt = ComponentCache.FindObjectsOfType<StationLogic>().SelectRandom();
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

        ComponentCache.FindObjectOfType<Simulation>().Refresh();

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
    }
    #endregion

    #region IPostLoadAsync
    public async Task OnPostLoadAsync()
    {
        // We need to load into the current system after game save load is complete
        await this.LoadSystemAsync(null, this.currentSystem);
        
        if (this.dockTargetBodyRef != null)
        {
            var dockTarget = ComponentCache.FindObjectsOfType<BodyGenerator>().FirstOrDefault(b => b.BodyRef == this.dockTargetBodyRef);
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
