// unset

using System;
using TMPro;
using UnityEngine;

public class FactionInfoUI : MonoBehaviour
{
    [SerializeField] private TMP_Text label = null;

    [SerializeField] private FactionExpansion factionExpansion = null;

    private void Update()
    {
        this.label.text = 
            $"<b><color=#777777>{this.factionExpansion.resourceAccumulated:0} / {this.factionExpansion.resourceForExpansion:0}</color></b> resources (+<b><color=#777777>{this.factionExpansion.resourceExpansionGrowthLastTick}</color></b>)\n" + 
            (this.factionExpansion.canExpand ? "Ready to expand, check mission board\n" : "") +
            $"<b><color=#00AAAA>{100 * this.factionExpansion.yieldFulfillment.energy:0}%</color></b> energy\n" +
            $"<b><color=#7777AA>{100 * this.factionExpansion.yieldFulfillment.pop:0}%</color></b> population\n"
            //$"<b><color=#00AAAA>{this.factionExpansion.yieldIn.energy:0} / {this.factionExpansion.yieldOut.energy:0}</color></b> energy\n" +
            //$"<b><color=#7777AA>{this.factionExpansion.yieldIn.pop:0} / {this.factionExpansion.yieldOut.pop:0}</color></b> population\n"
            ;
    }
}