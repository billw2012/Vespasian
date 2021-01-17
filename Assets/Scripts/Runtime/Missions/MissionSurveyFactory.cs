using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MissionSurveyFactory : MonoBehaviour, IMissionFactory, ISavable
{
    public GameObject itemPrefab;

    private void Awake()
    {
        FindObjectOfType<SaveSystem>().RegisterForSaving(this);
    }

    private enum SurveyType
    {
        SurveyWholeSystem,
        SurveyPlanet // Not supported yet
    }

    public IMissionBase Generate(RandomX rng)
    {
        return this.Generate(rng, null);
    }

    public IMissionBase Generate(RandomX rng, SolarSystem targetSystem = null)
    {
        var map = FindObjectOfType<MapComponent>().map;
        Debug.Assert(map != null);
        var missionType = SurveyType.SurveyWholeSystem;

        if (missionType == SurveyType.SurveyWholeSystem)
        {
            // TODO make good criteria for the generator
            if (targetSystem == null)
                targetSystem = map.systems.SelectRandom();

            string missionName = $"Survey System {targetSystem.id}";
            string missionDescription = $"Survey all bodies in system {targetSystem.id}";
            var mission = new MissionSurveySystem(missionDescription, missionName);
            var targetBodies = new List<BodyRef>();
            var targetSystemBodies = targetSystem.AllBodies().Where(b => b is StarOrPlanet);
            targetBodies.AddRange(targetBodies);

            mission.TargetBodies = targetBodies;
            mission.targetSystemRef = new BodyRef(targetSystem.id);

            return mission;
        }

        return null;
    }

    public GameObject CreateBoardUI(Missions missions, IMissionBase mission, Transform parent)
    {
        var missionTyped = mission as MissionSurveySystem;
        var ui = Instantiate(this.itemPrefab, parent);
        var missionItemUI = ui.GetComponent<MissionItemUI>();
        missionItemUI.Init(missions, mission, activeMission: false);
        return ui;
    }

    public GameObject CreateActiveUI(Missions missions, IMissionBase mission, Transform parent)
    {
        var missionTyped = mission as MissionSurveySystem;
        var ui = Instantiate(this.itemPrefab, parent);
        var missionItemUI = ui.GetComponent<MissionItemUI>();
        missionItemUI.Init(missions, mission, activeMission: true);
        return ui;
    }
}

// Mission for surveying the whole system
[RegisterSavableType]
public class MissionSurveySystem : IMissionBase, ITargetBodiesMission
{
    public bool IsComplete { get; set; }

    public string Description { get; set; }

    public string Name { get; set; }

    public int Reward => 100; // TODO proper reward value, based on distance

    public IEnumerable<BodyRef> TargetBodies { get; set; }

    public string Factory => nameof(MissionSurveyFactory);

    public BodyRef targetSystemRef;

    public MissionSurveySystem() { }

    public MissionSurveySystem(string description, string name)
    {
        this.Description = description;
        this.Name = name;
    }

    public void Update(Missions missions)
    {

    }

    public bool OnDataAdded(BodyRef bodyRef, Body body, DataMask data)
    {
        bool complete = false;
        if (bodyRef.EqualsSystem(this.targetSystemRef))
        {
            // Check if there are no unscanned bodies here any more
            // If so, mission is complete
            var missions = UnityEngine.Object.FindObjectOfType<Missions>();
            var playerDataCatalog = missions.playerDataCatalog;
            bool notAllScanned = this.TargetBodies.FirstOrDefault<BodyRef>(tb => {
                var dataOnThisBody = playerDataCatalog.GetData(tb);
                return !dataOnThisBody.HasFlag(DataMask.All);
            }) != null;
            if (!notAllScanned)
            {
                complete = true;
            }
        }
        this.IsComplete = complete;
        return complete;
    }
}