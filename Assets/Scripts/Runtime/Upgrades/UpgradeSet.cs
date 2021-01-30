using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu]
public class UpgradeSet : ScriptableObject
{
    public List<UpgradeDef> upgradesDefs;

    public UpgradeDef GetUpgradeDef(string name) => this.upgradesDefs.FirstOrDefault(u => u.name == name);
}
