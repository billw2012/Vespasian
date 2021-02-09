using IngameDebugConsole;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;


/*
 * What is a mission?
 * In the most general terms it is an action that will occur when a predicate is satisfied. 
 * A set of constraints that must be full-filled.
 * Some form of reward for doing it would be expected, but not necessary.
 * 
 * Predicate examples:
 * - player has data on a planet that full-fills certain criteria
 * - player is in a certain system
 * - player is traveling at a certain speed, etc.
 * - player deploys a satellite to a certain orbit around a certain body
 * Predicate composition would follow normal boolean logical forms, e.g. and / or / not, etc.
 * Predicates must also translate into a description of the requirements to the player.
 * 
 * Choice for abstraction is either:
 * - to fully abstract to a single predicate function and description function,
 * then specialize it for each mission type (with some parameterization in the specializations themselves).
 * - to design a DSL for describing the predicate, allowing automatic generation of the description
 * 
 * However the first option allows the second to be a subset of specialization anyway, so is probably a 
 * good start.
 * 
 * What is needed for missions?
 * - mission descriptions themselves
 *      this class
 * - some way to get missions
 *      can start with a global list, but eventually only accessible from certain places
 * - a record of active missions
 *      probably NOT on the player ship as the player is separate from their ship, make it global for now?
 *      some ui layer the player can access at any time to see this list
 * - some way to hand in missions
 *      to start with can just be handed in from the active mission list when they are complete,
 *      but eventually only from certain places (probably the same place you get missions from)
 */

public interface IMissionFactory
{
    IMissionBase Generate(RandomX rng);
    GameObject CreateBoardUI(Missions missions, IMissionBase mission, Transform parent);
    GameObject CreateActiveUI(Missions missions, IMissionBase mission, Transform parent);
}

public interface IMissionBase
{
    string Factory { get; }
    bool IsComplete { get; }
    string Name { get; }
    string Description { get; }
    int Reward { get; }
    void Update(Missions missions);
}

public interface IBodyMission
{
    /// <summary>
    /// Try and assign the body to this mission (mission can check it matches requirements). 
    /// </summary>
    /// <param name="bodyRef"></param>
    /// <param name="body"></param>
    /// <param name="data"></param>
    /// <returns>Whether the body was assigned to this mission. If so it cannot be assigned to another one.</returns>
    /// TODO: Perhaps we should support a body assigned to multiple missions if they are for the same agent?
    bool TryAssign(BodyRef bodyRef, Body body, DataMask data);
    /// <summary>
    /// </summary>
    /// <returns>A list of all bodies assigned to this mission.</returns>
    IEnumerable<BodyRef> AssignedBodies();
}

// Mission which targets specific bodies which exist in the world
public interface ITargetBodiesMission
{
    List<BodyRef> TargetBodies { get; }
    bool OnDataAdded(BodyRef bodyRef, Body body, DataMask data);
}

public class Missions : MonoBehaviour, ISavable
{
    public List<GameObject> missionFactoryObjects;

    [NonSerialized, Saved, RegisterSavableType]
    public List<IMissionBase> activeMissions = new List<IMissionBase>();

    // This should perhaps be specific to particular locations, not sure?
    [NonSerialized, Saved, RegisterSavableType]
    public List<IMissionBase> availableMissions = new List<IMissionBase>();

    [NonSerialized, Saved] public int playerCredits;

    // Data already used to complete missions
    public DataCatalog dataCatalog;
    
    // Data the player knows
    public DataCatalog playerDataCatalog;

    public MapComponent mapComponent;

    public delegate void MissionsChanged();
    public event MissionsChanged OnMissionsChanged;

    // Externals
    public GameLogic gameLogic;
    
    private IEnumerable<IMissionFactory> missionFactories => this.missionFactoryObjects.Select(o => o.GetComponent<IMissionFactory>());

    private List<IMissionBase> completedMissions;

    public int NewDataReward { get; private set; }
    private DictX<BodyRef, DataMask> newData;

    // Start is called before the first frame update
    private void Awake()
    {
        FindObjectOfType<SaveSystem>().RegisterForSaving(this);
        
        this.playerDataCatalog.OnDataAdded += this.PlayerDataCatalogOnDataAdded;
        this.gameLogic.OnNewGameInitialized.AddListener(this.GameLogicOnNewGameInitialized);
    }

    private void Start()
    {
        this.completedMissions = this.activeMissions.Where(m => m.IsComplete).ToList();
    }

    private void PlayerDataCatalogOnDataAdded(BodyRef bodyRef, DataMask oldData, DataMask newData)
    {
        var bodyMissions = this.activeMissions.OrderByDescending(m => m.Reward).OfType<IBodyMission>();
        var allocatedBodies = bodyMissions
            .SelectMany(m => m.AssignedBodies())
            .Concat(this.dataCatalog.KnownBodies);

        var body = this.mapComponent.map.GetBody(bodyRef);
        Assert.IsNotNull(body);

        if (!allocatedBodies.Contains(bodyRef))
        {
            foreach (var mission in bodyMissions)
            {
                if (mission.TryAssign(bodyRef, body, newData))
                    break;
            }
        }

        // Check system missions
        // We only care if the added data corresponds to anything in this system
        var targetBodyMissions = this.activeMissions.OfType<ITargetBodiesMission>();
        foreach (var mission in targetBodyMissions)
        {
            // TODO: use LINQ to check if bodies in mission contain this body
            mission.OnDataAdded(bodyRef, body, newData);
        }
    }

    private void GameLogicOnNewGameInitialized()
    {
        var rng = new RandomX();

        // HACK: DEBUG CODE
        for (int i = 0; i < 10; i++)
        {
            this.availableMissions.Add(this.missionFactories.SelectRandom().Generate(rng));
        }

        // Generate survey system missions in neighboring systems too
        var surveyFactory = this.missionFactories.FirstOrDefault(f => f is MissionSurveyFactory) as MissionSurveyFactory;
        if (surveyFactory != null)
        {
            var map = this.mapComponent.map;
            var nearestSystems = map.GetConnected(this.mapComponent.currentSystem);
            this.availableMissions.Add(surveyFactory.Generate(rng, this.mapComponent.currentSystem));
            foreach (var systemAndLink in nearestSystems)
            {
                this.availableMissions.Add(surveyFactory.Generate(rng, systemAndLink.system));
            }
        }


        // Unsubscribe, although Unity should handle this case too
        this.gameLogic.OnNewGameInitialized.RemoveListener(this.GameLogicOnNewGameInitialized);
    }

    // Update is called once per frame
    private void Update()
    {
        foreach(var mission in this.activeMissions)
        {
            mission.Update(this);
            if (mission.IsComplete && !this.completedMissions.Contains(mission))
            {
                NotificationsUI.Add($"<style=mission>Mission <b>{mission.Name}</b> complete!</style>");
                this.completedMissions.Add(mission);
            }
        }

        // Cull available missions
        // Generate new missions
    }

    public IMissionFactory GetFactory(string name) => this.missionFactories.First(f => f.GetType().Name == name);
    
    public void Take(IMissionBase mission)
    {
        this.availableMissions.Remove(mission);
        this.activeMissions.Add(mission);
        this.OnMissionsChanged?.Invoke();
    }

    public void HandIn(IMissionBase mission)
    {
        this.activeMissions.Remove(mission);
        this.OnMissionsChanged?.Invoke();

        this.AddFunds(mission.Reward, $"<style=mission>Mission <b>{mission.Name}</b> completed</style>");
    }

    public void AddFunds(int amount, string reason)
    {
        Assert.IsTrue(amount > 0, "Can only add positive funds, use SubtractFunds to remove funds...");
        this.playerCredits += amount;
        NotificationsUI.Add($"{reason}, +<style=credits>{amount} cr</style>");
    }
    
    public void SubtractFunds(int amount)
    {
        Assert.IsTrue(this.playerCredits >= amount, "Not enough credits!");
        this.playerCredits -= amount;
        NotificationsUI.Add($"-<style=credits-bad>{amount} cr</style>");
    }

    public void UpdateNewDataReward()
    {
        this.newData = this.playerDataCatalog.GetNewDataDiff(this.dataCatalog);
        this.NewDataReward = this.newData
            .Select(bodyData => this.mapComponent.GetDataCreditValue(bodyData.Key, bodyData.Value))
            .Sum();
    }

    public void SellNewData()
    {
        this.dataCatalog.MergeFrom(this.playerDataCatalog);
        this.AddFunds(this.NewDataReward, $"All new data sold");
    }

    public bool HasActiveMissionsInSystem(BodyRef systemRef)
    {
        var firstSurveyMission = this.activeMissions.FirstOrDefault(mn =>
            {
                var mnSurvey = mn as MissionSurvey;

                if (mnSurvey != null)
                {
                    return mnSurvey.targetSystemRef.EqualsSystem(systemRef) && !mnSurvey.IsComplete;
                }
                else
                    return false;
            }
        );
        return firstSurveyMission != null;
    }

    public List<IMissionBase> GetActiveMissionsInSystem(BodyRef systemRef)
    {
        var surveySystemMissionsHere = this.activeMissions.Where(mn =>
            {
                var mnSurvey = mn as MissionSurvey;

                if (mnSurvey != null)
                {
                    return mnSurvey.targetSystemRef.EqualsSystem(systemRef) && !mnSurvey.IsComplete;
                }
                else
                    return false;
            }
        );
        return surveySystemMissionsHere.ToList();
    }
    
    [ConsoleMethod("player.reveal-map", "Reveal all planets to the player")]
    public static void DebugPlayerRevealMap(string dataMask = null)
    {
        var missions = FindObjectOfType<Missions>();
        var map = FindObjectOfType<MapComponent>()?.map;
        if (missions != null && map != null)
        {
            foreach (var body in map.systems.SelectMany(s => s.AllBodies().OfType<StarOrPlanet>()))
            {
                missions.playerDataCatalog.AddData(body.bodyRef, dataMask != null? (DataMask)Enum.Parse(typeof(DataMask), dataMask, ignoreCase: true) : DataMask.All);
            }
        }
    }

    [ConsoleMethod("player.scan-system", "Scans all bodies in current system")]
    public static void DebugPlayerScanSystem()
    {
        var missions = FindObjectOfType<Missions>();
        var mapComponent = FindObjectOfType<MapComponent>();

        if (missions != null && mapComponent != null)
        {
            var scannables = ComponentCache.FindObjectsOfType<Scannable>();
            foreach (var i in scannables)
                missions.playerDataCatalog.AddData(i.gameObject, DataMask.All);
        }
    }

    [ConsoleMethod("player.give-credits", "Give specified amount of credits to player")]
    public static void DebugPlayerGiveCredits(int amount)
    {
        var missions = FindObjectOfType<Missions>();
        if (missions != null)
        {
            missions.AddFunds(amount, $"Cheating!");
        }
    }
}
