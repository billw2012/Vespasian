using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class UpgradeItemUI : MonoBehaviour
{
    public Button installButton;
    public GameObject installedTick;
    public TMP_Text nameLabel;
    public TMP_Text costLabel;

    private UpgradeDef upgradeDef;
    private UpgradeUI upgradeUI;
    private Missions missions;
    
    public void Init(UpgradeUI upgradeUI, UpgradeDef upgradeDef, Missions missions)
    {
        this.upgradeDef = upgradeDef;
        this.upgradeUI = upgradeUI;
        this.missions = missions;

        this.UpdateState();
    }

    public void Install()
    {
        this.upgradeUI.Install(this.upgradeDef);
    }

    public void UpdateState()
    {
        bool canAfford = this.missions.playerCredits >= this.upgradeDef.cost;
        bool isInstalled = this.upgradeUI.IsInstalled(this.upgradeDef);
        this.installButton.interactable = this.upgradeUI.CanInstall(this.upgradeDef) && canAfford;
        this.installedTick.SetActive(isInstalled);
        this.nameLabel.text = this.upgradeDef.name;
        this.costLabel.text = isInstalled? "" : $"{this.upgradeDef.cost} cr";
        this.costLabel.color = canAfford? new Color(1f, 1f, 0.1789966f) : new Color(1f, 0.1875373f, 0f);
    }
}
