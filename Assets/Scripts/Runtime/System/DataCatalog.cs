using System;
using System.Collections.Generic;
using UnityEngine;

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
    [Saved, RegisterSavableType]
    private DictX<BodyRef, DataMask> data = new DictX<BodyRef, DataMask>();

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

    public DictX<BodyRef, DataMask> GetNewDataDiff(DataCatalog baseCatalog)
    {
        var newData = new DictX<BodyRef, DataMask>();
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
