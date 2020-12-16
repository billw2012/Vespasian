using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MissionFindFactory : MonoBehaviour, IMissionFactory, ISavable
{
    public GameObject boardUIPrefab;
    public GameObject activeUIPrefab;

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
        var ui = Instantiate(this.boardUIPrefab, parent);
        ui.transform.Find("Description").GetComponent<TMP_Text>().text = $"{missionTyped.Name}\n{ missionTyped.Description}";
        ui.transform.Find("Take").GetComponent<Button>().onClick.AddListener(() => missions.Take(mission));
        return ui;
    }

    public GameObject CreateActiveUI(Missions missions, IMissionBase mission, Transform parent)
    {
        var missionTyped = mission as MissionFind;
        var ui = Instantiate(this.activeUIPrefab, parent);
        ui.transform.Find("Description").GetComponent<TMP_Text>().text = $"{missionTyped.Name}\n{ missionTyped.Description}";
        var handInButton = ui.transform.Find("Hand In").GetComponent<Button>();
        handInButton.onClick.AddListener(() => missions.HandIn(mission));
        bool docked = this.player.GetComponentInChildren<DockActive>()?.docked ?? false;
        handInButton.gameObject.SetActive(mission.IsComplete && docked);
        return ui;
    }
}

[RegisterSavableType]
public class MissionFind : IMissionBase, IBodyMission
{
    public bool IsComplete { get; set; }
    
    public string Description { get; set; }

    public string Name { get; set; }

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
