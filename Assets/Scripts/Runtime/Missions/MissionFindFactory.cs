﻿using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MissionFindFactory : MonoBehaviour, IMissionFactory, ISavable
{
    public GameObject itemPrefab;

    [Saved]
    [NonSerialized]
    public int missionCounter = 0;

    private PlayerController player;

    private void Awake()
    {
        FindObjectOfType<SaveSystem>().RegisterForSaving(this);
        this.player = FindObjectOfType<PlayerController>();
    }

    private enum FindType
    {
        Resource,
        Habitability,
        Both
    }
    public IMissionBase Generate(RandomX rng)
    {
        // Generate resource only, hab only, or mixed missions
        var missionType = (FindType)rng.Range(0, 3);
        float minResource = rng.value;
        float minHabitability = rng.value;
        string missionName = $"Find Mission {++this.missionCounter}";
        switch (missionType)
        {
            case FindType.Resource:
                return new MissionFind(
                    $"Find resource >= {minResource}",
                    missionName
                    )
                {
                    MinResource = minResource
                };
            case FindType.Habitability:
                return new MissionFind(
                    $"Find habitability >= {minHabitability}",
                    missionName
                    )
                {
                    MinHabitability = minHabitability
                };

            case FindType.Both:
            default:
                return new MissionFind(
                    $"Find resource >= {minResource}\nFind habitability >= {minHabitability}",
                    missionName
                    )
                {
                    MinResource = minResource,
                    MinHabitability = minHabitability
                };
        }
    }

    public GameObject CreateBoardUI(Missions missions, IMissionBase mission, Transform parent)
    {
        var missionTyped = mission as MissionFind;
        var ui = Instantiate(this.itemPrefab, parent);
        var missionItemUI = ui.GetComponent<MissionItemUI>();
        missionItemUI.Init(missions, mission, activeMission: false);
        return ui;
    }

    public GameObject CreateActiveUI(Missions missions, IMissionBase mission, Transform parent)
    {
        var missionTyped = mission as MissionFind;
        var ui = Instantiate(this.itemPrefab, parent);
        var missionItemUI = ui.GetComponent<MissionItemUI>();
        missionItemUI.Init(missions, mission, activeMission: true);
        return ui;
    }
}

[RegisterSavableType]
public class MissionFind : IMissionBase, IBodyMission
{
    public bool IsComplete { get; set; }
    
    public string Description { get; set; }

    public string Name { get; set; }

    public int Reward => 20 + Mathf.CeilToInt(this.MinHabitability * 100f + this.MinResource * 50f);

    public float MinResource { get; set; }

    public float MinHabitability { get; set; }

    public BodyRef AssignedBody { get; set; }
    
    public string Factory => nameof(MissionFindFactory);

    public MissionFind() { }

    public MissionFind(string description, string name)
    {
        this.Description = description;
        this.Name = name;
    }

    public void Update(Missions missions)
    {
        
    }

    public bool TryAssign(BodyRef bodyRef, Body body, DataMask data)
    {
        // Only need as single body to complete this mission, so if we already have one we can leave
        if (this.AssignedBody != null)
        {
            return false;
        }
        // Has to be a planet
        if (!(body is StarOrPlanet))
        {
            return false;
        }
        var planet = (StarOrPlanet) body;
        // We want to match both requirements, but if a requirement is 0 then we don't need to have
        // the data about it
        if ((this.MinHabitability == 0 || data.HasFlag(DataMask.Habitability)) && planet.habitability >= this.MinHabitability &&
            (this.MinResource == 0 || data.HasFlag(DataMask.Resources)) && planet.resources >= this.MinResource)
        {
            this.AssignedBody = bodyRef;
            this.IsComplete = true;
            return true;
        }

        return false;
    }

    public IEnumerable<BodyRef> AssignedBodies() => new[] {this.AssignedBody};
}