using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

[CreateAssetMenu]
public class UpgradeDef : ScriptableObject
{
    public int cost = 0;
    public float mass = 0;

    public GameObject shipPartPrefab;
    public GameObject shopUIPrefab;

    public List<UpgradeDef> replaces;
    public List<UpgradeDef> requires;

    public void AddUI(RectTransform parent)
    {
        var obj = Object.Instantiate(this.shopUIPrefab, parent);
        obj.name = this.name;
    }
}
