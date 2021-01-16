using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * How does data work?
 * Requirements:
 *  - Player gathers data that completes missions, perhaps they can sell it raw as well
 *  - Therefore other agents also have data sets, possibly multiple ones
 *  - For each transaction (mission completion, data trading) we need to know what data the player
 *    has that the agent doesn't, so we need to diff it.
 *    For one off trading of data this can be slow, but for missions we need to update them quickly.
 *    The operation we want to be fast is determining the list of new data wrt any specific agent.
 *    If all agents are known then we can accumulate new data into separate lists for each one.
 *    Do we know all agents? Yes we must, if an agent can have a persistent list of what it DOES know then
 *    we must "know" the agent.
 *    Ergo we could register all agents directly with a central data discovery manager, then as the player discovers
 *    data it is added to a pending list for each agent that can separately consume data (probably only 2 of them).
 *    Then it can work like this:
 *    - Missions are of course directly associated with an agent (probably just a general mission agent to begin with)
 *    - When mission updates it can check for a matching piece of data in the pending list and complete it using the
 *      data, immediately moving it to the discovered list for the agent. The mission remains in the players list
 *      until it is handed in to get the reward.
 *      OKAY that seems a bit weird. Would be nicer if the mission takes the data itself, and the agent only gets it
 *      when its handed in...
 *
 * TODO:
 * - Any body can only be assigned to complete one mission, and must not be discovered at all yet
 * - DataCatalog in Missions
 * - Events in DataCatalog
 * - 
 */

/// <summary>
/// Represents data about a body.
/// </summary>
[Flags]
public enum DataMask : uint
{
    None = 0,
    Orbit = 1 << 0,
    Basic = 1 << 1,
    Composition = 1 << 2,
    Resources = 1 << 3,
    Habitability = 1 << 4,
    Last = 1 << 4,
    Count = 4,
    All = Orbit | Basic | Composition | Resources | Habitability,
    __Fix = 1 << 5, // Work around for Unity bug (https://forum.unity.com/threads/enum-flags-everything-option-not-working-with-unsigned-integers.953979/)
}

/// <summary>
/// Store a set of data related to system bodies, keyed by system ID and body ID
/// </summary>
public class DataCatalog : MonoBehaviour, ISavable
{
    // Each body in the universe is looked up by <system index n universe, body index in system>.
    // Each one can have 0-n bits of known data in the catalog.
    
    // TODO: optimization
    // Requirements are to be able to list all data for a specific system, hence a nested dict. However perhaps jagged array
    // could work better if body indices are made to be 0 based for each system (currently they aren't).
    // Or even just Array<Array<(id, data)>>, as body count in systems is low enough for linear search to be fast
    // (could do binary actually, just sort ids)
    //private Dictionary<int, Dictionary<int, DataMask>> data = new Dictionary<int, Dictionary<int, DataMask>>();
    private Dictionary<BodyRef, DataMask> data = new Dictionary<BodyRef, DataMask>();

    public IEnumerable<BodyRef> KnownBodies => this.data.Keys;
    
    public delegate void DataAdded(BodyRef bodyRef, DataMask oldData, DataMask newData);

    public event DataAdded OnDataAdded;
    
    public bool HaveData(BodyRef bodyRef, DataMask dataMask) =>
        this.data.TryGetValue(bodyRef, out var foundMask)
        && (dataMask & foundMask) == dataMask;

    public DataMask GetData(BodyRef bodyRef)
    {
        if (this.data.TryGetValue(bodyRef, out var existingDataMask))
        {
            return existingDataMask;
        }
        else
        {
            return DataMask.None;
        }
    }

    public DataMask GetData(int systemId, int bodyId) => this.GetData(new BodyRef(systemId, bodyId));

    public bool AddData(GameObject obj, DataMask newDataMask)
    {
        var bodyGenerator = obj.GetComponent<BodyGenerator>();
        if (bodyGenerator != null && bodyGenerator.BodyRef != null)
        {
            // We only discover orbit by default
            this.AddData(bodyGenerator.BodyRef, newDataMask);
            return true;
        }
        else
        {
            return false;
        }
    }

    public void AddData(BodyRef bodyRef, DataMask newDataMask)
    {
        if (!this.data.TryGetValue(bodyRef, out var existingDataMask))
        {
            this.data.Add(bodyRef, newDataMask);
            this.OnDataAdded?.Invoke(bodyRef, DataMask.None, newDataMask);
        }
        else
        {
            newDataMask |= existingDataMask;
            this.data[bodyRef] = newDataMask;
            this.OnDataAdded?.Invoke(bodyRef, existingDataMask, newDataMask);
        }
        Debug.Log($"{bodyRef}:{newDataMask} was added to DataCatalog of {this.gameObject.name}");
    }

    /// <summary>
    /// What data exists in this catalog but not in <paramref name="baseCatalog"/>
    /// </summary>
    /// <param name="systemId"></param>
    /// <param name="bodyId"></param>
    /// <param name="baseCatalog"></param>
    /// <returns></returns>
    public DataMask GetNewDataDiff(BodyRef bodyRef, DataCatalog baseCatalog) => ~baseCatalog.GetData(bodyRef) & this.GetData(bodyRef);

    public Dictionary<BodyRef, DataMask> GetNewDataDiff(DataCatalog baseCatalog)
    {
        var newData = new Dictionary<BodyRef, DataMask>();
        foreach (var bodyData in this.data)
        {
            var newDataMask = bodyData.Value & ~baseCatalog.GetData(bodyData.Key);
            if (newDataMask != DataMask.None)
            {
                newData.Add(bodyData.Key, newDataMask);
            }
        }

        return newData;
    }
    
    public void MergeFrom(DataCatalog from, DataMask dataMask = DataMask.All)
    {
        foreach (var bodyData in from.data)
        {
            var newDataMasked = bodyData.Value & dataMask;
            if(newDataMasked != DataMask.None)
            {
                this.AddData(bodyData.Key, newDataMasked);
            }
        }
    }
}
