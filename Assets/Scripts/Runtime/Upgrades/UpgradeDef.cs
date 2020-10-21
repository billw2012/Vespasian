using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// Defines a possible ship upgrade.
/// </summary>
[CreateAssetMenu]
public class UpgradeDef : ScriptableObject
{
    public int cost = 0;
    public float mass = 0;

    /// <summary>
    /// This prefab will be instanced into the ship UpgradeRoot.
    /// It must implement IUpgradeLogic in one of the root components.
    /// </summary>
    public GameObject shipPartPrefab;
    /// <summary>
    /// This prefab, if it exists, will be instanced in the UI.
    /// </summary>
    public GameObject shopUIPrefab;

    /// <summary>
    /// What upgrades this one will replace.
    /// </summary>
    public List<UpgradeDef> replaces;
    /// <summary>
    /// What other upgrades this one requires.
    /// </summary>
    public List<UpgradeDef> requires;

    /// <summary>
    /// Instance the UI prefab into the provided container.
    /// </summary>
    /// <param name="parent"></param>
    public void AddUI(RectTransform parent)
    {
        var obj = Object.Instantiate(this.shopUIPrefab, parent);
        obj.name = this.name;
    }
}
