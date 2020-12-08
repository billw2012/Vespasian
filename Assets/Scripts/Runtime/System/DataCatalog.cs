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
    private Dictionary<int, Dictionary<int, DataMask>> data = new Dictionary<int, Dictionary<int, DataMask>>();

    public bool HaveData(int systemId, int bodyId, DataMask dataMask) =>
        this.data.TryGetValue(systemId, out var systemData)
        && systemData.TryGetValue(bodyId, out var foundMask)
        && (dataMask & foundMask) == dataMask;

    public DataMask GetData(int systemId, int bodyId)
    {
        if (this.data.TryGetValue(systemId, out var systemData) && systemData.TryGetValue(bodyId, out var existingDataMask))
        {
            return existingDataMask;
        }
        else
        {
            return DataMask.None;
        }
    }

    public bool AddData(GameObject obj, DataMask newDataMask)
    {
        var bodyGenerator = obj.GetComponent<BodyGenerator>();
        if (bodyGenerator != null)
        {
            // We only discover orbit by default
            this.AddData(bodyGenerator.system.id, bodyGenerator.body.id, newDataMask);
            return true;
        }
        else
        {
            return false;
        }
    }

    public void AddData(int systemId, int bodyId, DataMask newDataMask)
    {
        if (!this.data.TryGetValue(systemId, out var systemData))
        {
            systemData = new Dictionary<int, DataMask>();
            this.data.Add(systemId, systemData);
        }
        if (!systemData.TryGetValue(bodyId, out var existingDataMask))
        {
            systemData.Add(bodyId, newDataMask);
        }
        else
        {
            existingDataMask |= newDataMask;
            systemData[bodyId] = existingDataMask;
        }
        Debug.Log($"{newDataMask} was added to DataCatalog of {this.gameObject.name}");
    }

    /// <summary>
    /// What data exists in this catalog but not in <paramref name="baseCatalog"/>
    /// </summary>
    /// <param name="systemId"></param>
    /// <param name="bodyId"></param>
    /// <param name="baseCatalog"></param>
    /// <returns></returns>
    public DataMask GetNewDataDiff(int systemId, int bodyId, DataCatalog baseCatalog) => ~baseCatalog.GetData(systemId, bodyId) & this.GetData(systemId, bodyId);

    public void MergeFrom(DataCatalog from, DataMask dataMask)
    {
        foreach (var systemData in from.data)
        {
            foreach (var bodyData in systemData.Value)
            {
                var newDataMasked = bodyData.Value & dataMask;
                if(newDataMasked != DataMask.None)
                {
                    this.AddData(systemData.Key, bodyData.Key, newDataMasked);
                }
            }
        }
    }
}
