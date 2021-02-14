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
            $"<b><color=#777777>{this.factionExpansion.resources:0}</color></b> resources\n" +
            $"<b><color=#00AAAA>{this.factionExpansion.energy:0}</color></b> energy\n" +
            $"<b><color=#7777AA>{this.factionExpansion.population:0}</color></b> population\n"
            ;
    }
}