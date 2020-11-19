using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


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
    IMissionBase Generate();
    GameObject CreateBoardUI(Missions missions, IMissionBase mission, Transform parent);
    GameObject CreateActiveUI(Missions missions, IMissionBase mission, Transform parent);
}

public interface IMissionBase
{
    string Factory { get; }
    bool IsComplete { get; }
    void Update();
}

public class Missions : MonoBehaviour, ISavable
{
    public List<GameObject> missionFactoryObjects;
    IEnumerable<IMissionFactory> missionFactories => this.missionFactoryObjects.Select(o => o.GetComponent<IMissionFactory>());

    [NonSerialized]
    [Saved]
    [RegisterSavableType]
    public List<IMissionBase> activeMissions = new List<IMissionBase>();

    // This should perhaps be specific to particular locations, not sure?
    [NonSerialized]
    [Saved]
    [RegisterSavableType]
    public List<IMissionBase> availableMissions = new List<IMissionBase>();


    public delegate void MissionsChanged();
    public event MissionsChanged OnMissionsChanged;

    // Start is called before the first frame update
    void Awake()
    {
        // HACK: DEBUG CODE
        for (int i = 0; i < 10; i++)
        {
            this.availableMissions.Add(this.missionFactories.SelectRandom().Generate());
        }

        FindObjectOfType<SaveSystem>().RegisterForSaving(this);
    }

    // Update is called once per frame
    void Update()
    {
        foreach(var mission in this.activeMissions)
        {
            mission.Update();
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
    }
}
