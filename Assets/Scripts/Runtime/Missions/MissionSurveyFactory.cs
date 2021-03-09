using System.Collections.Generic;
using System.Linq;
using UnityEngine;

//MissionSurvery(SomethingSpecific)
public class MissionSurveyFactory : MonoBehaviour, IMissionFactory, ISavable
{
    [SerializeField] private GameObject itemPrefab = null;

    private readonly RandomX rng = new RandomX();
    
    private MapComponent mapComponent;
    
    private void Awake()
    {
        ComponentCache.FindObjectOfType<SaveSystem>().RegisterForSaving(this);
        this.mapComponent = ComponentCache.FindObjectOfType<MapComponent>();
    }

    private enum SurveyType
    {
        SurveyWholeSystem,
        SurveyPlanet // Not supported yet
    }

    private IMissionBase Generate(SolarSystem targetSystem)
    {
        var missionType = this.rng.Decide(0.5f) ? SurveyType.SurveyWholeSystem : SurveyType.SurveyPlanet;

        switch (missionType)
        {
            case SurveyType.SurveyWholeSystem:
            {
                // TODO make good criteria for the generator
                string missionName = $"Survey System {targetSystem.name}";
                string missionDescription = $"Survey all bodies in system {targetSystem.name}";
                var mission = new MissionSurveySystem(missionDescription, missionName)
                {
                    TargetBodies = targetSystem.AllBodies().Where(b => b is StarOrPlanet)
                        .Select(b => b.bodyRef)
                        .ToList(),
                    targetSystemRef = targetSystem.id
                };

                return mission;
            }

            case SurveyType.SurveyPlanet:
            {
                var targetBody = targetSystem.AllBodies()
                    .Where(b => b is StarOrPlanet)
                    .SelectRandom();

                string missionName = $"Survey Planet {targetBody.name}";
                string missionDescription = $"Survey planet {targetBody.name} located in system {targetSystem.name}";

                var mission = new MissionSurveyBody(missionDescription, missionName)
                {
                    TargetBodies = new List<BodyRef> {targetBody.bodyRef},
                    targetSystemRef = targetBody.bodyRef.SystemRef()
                };

                return mission;
            }
        }
        return null;
    }

    #region IMissionFactory
    public IEnumerable<IMissionBase> GetMissions(Missions missions)
    {
        // Which systems are already part of active missions?
        var existingTargetSystems = missions.activeMissions
            .OfType<MissionSurvey>()
            .Select(m => m.targetSystemRef)
            .ToList();
        
        if(!existingTargetSystems.Contains(this.mapComponent.currentSystem.id))
        {
            yield return this.Generate(this.mapComponent.currentSystem);
        }
        foreach (var systemAndLink in this.mapComponent.map.GetConnected(this.mapComponent.currentSystem)
            .Where(sl => !existingTargetSystems.Contains(sl.system.id)))
        {
            yield return this.Generate(systemAndLink.system);
        }
    }

    public void MissionTaken(Missions missions, IMissionBase mission) { }

    public GameObject CreateBoardUI(Missions missions, IMissionBase mission, Transform parent)
    {
        var missionTyped = mission as MissionSurveySystem;
        var ui = ComponentCache.Instantiate(this.itemPrefab, parent);
        var missionItemUI = ui.GetComponent<MissionItemUI>();
        missionItemUI.Init(missions, mission, activeMission: false);
        return ui;
    }

    public GameObject CreateActiveUI(Missions missions, IMissionBase mission, Transform parent)
    {
        var missionTyped = mission as MissionSurveySystem;
        var ui = ComponentCache.Instantiate(this.itemPrefab, parent);
        var missionItemUI = ui.GetComponent<MissionItemUI>();
        missionItemUI.Init(missions, mission, activeMission: true);
        return ui;
    }

    #endregion IMissionFactory
}

public abstract class MissionSurvey : IMissionBase, ITargetBodiesMission
{
    public bool IsComplete { get; private set; }
    public string Description { get; }
    public string Name { get; }

    public int Reward => 100; // TODO proper reward value, based on distance

    public string Factory => nameof(MissionSurveyFactory);

    // Ref to the system where target is (or the target system, if target is a system not a body)
    public BodyRef targetSystemRef;

    private List<BodyRef> notScannedBodies = new List<BodyRef>();
    private List<BodyRef> targetBodies = null;

    public IEnumerable<BodyRef> TargetBodies
    {
        get => this.targetBodies ?? Enumerable.Empty<BodyRef>();
        set
        {
            this.notScannedBodies = new List<BodyRef>(value);
            this.targetBodies = new List<BodyRef>(value);
        }
    }

    // Parameterless constructor to allow serialization
    protected MissionSurvey() { }

    protected MissionSurvey(string description, string name)
    {
        this.Description = description;
        this.Name = name;
    }

    public void Update(Missions missions) { }

    public bool OnDataAdded(BodyRef bodyRef, Body body, DataMask data)
    {
        if (bodyRef.EqualsSystem(this.targetSystemRef))
        {
            // Check if there are no unscanned bodies here any more
            // If so, mission is complete
            var playerDataCatalog = ComponentCache.FindObjectOfType<Missions>().playerDataCatalog;

            // Check if we now have full data on this body
            if (playerDataCatalog.HaveData(bodyRef, DataMask.All))
            {
                this.notScannedBodies.RemoveAll(b=> b == bodyRef);
                this.IsComplete = this.TargetBodies.All(tb => playerDataCatalog.HaveData(tb, DataMask.All));
            }
        }
        return this.IsComplete;
    }
}

// Mission for surveying the whole system
[RegisterSavableType]
public class MissionSurveySystem : MissionSurvey //IMissionBase, ITargetBodiesMission
{
    // Parameterless constructor to allow serialization
    public MissionSurveySystem() { }
    public MissionSurveySystem(string description, string name) : base(description, name) {}
}

// Mission for surveying just one body in the system
[RegisterSavableType]
public class MissionSurveyBody : MissionSurvey
{
    // Parameterless constructor to allow serialization
    public MissionSurveyBody() { }
    public MissionSurveyBody(string description, string name) : base(description, name) { }
}