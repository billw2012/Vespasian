// unset

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MissionExpandFactory : MonoBehaviour, IMissionFactory
{
    [SerializeField] private GameObject itemPrefab = null;
    [SerializeField] private FactionExpansion factionExpansion = null;
    
    public IEnumerable<IMissionBase> GetMissions(Missions missions)
    {
        if (missions.activeMissions.OfType<MissionExpand>().Any())
        {
            return Enumerable.Empty<IMissionBase>();
        }
        else
        {
            var map = ComponentCache.FindObjectOfType<MapComponent>().map;

            string StationTypeDesc(BodySpecs.StationType type)
            {
                switch (type)
                {
                    case BodySpecs.StationType.HomeStation:      return "Home Base";
                    case BodySpecs.StationType.MiningStation:    return "Mining";
                    case BodySpecs.StationType.CollectorStation: return "Energy";
                    case BodySpecs.StationType.HabitatStation:   
                    default:                                     return "Habitat";
                }
            }
            // DOING: implement the mission type for expansion, and the rest of the factory, add it to scene
            return this.factionExpansion.expansionTargets.Select(t => new MissionExpand(
                $"Build {StationTypeDesc(t.type)} at {map.GetBody(t.parent).name}",
                "Faction Expansion",
                t.parent,
                t.score,
                t.type
                ));
        }
    }

    public void MissionTaken(Missions missions, IMissionBase mission) {}

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
}

[RegisterSavableType]
public class MissionExpand : IMissionBase, ITargetBodiesMission
{
    public bool IsComplete { get; set; }
    
    public string Description { get; }

    public string Name { get; }

    public int Reward => (int) (200 + this.TargetValue * 300);

    public BodyRef TargetBody { get; set; }

    private float TargetValue { get; set; }
    private BodySpecs.StationType StationType { get; set; }
    
    public string Factory => nameof(MissionExpandFactory);

    public MissionExpand() { }

    public MissionExpand(string description, string name, BodyRef targetBody, float score, BodySpecs.StationType type)
    {
        this.Description = description;
        this.Name = name;
        this.TargetBody = targetBody;
        this.TargetValue = score;
        this.StationType = type;
    }

    public void Update(Missions missions)
    {
        
    }

    #region ITargetBodiesMission
    public IEnumerable<BodyRef> TargetBodies => this.TargetBody.Yield();
    public bool OnDataAdded(BodyRef bodyRef, Body body, DataMask data) => false;
    #endregion ITargetBodiesMission
}
