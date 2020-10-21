using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeItemUI : MonoBehaviour
{
    public Button installButton;
    public GameObject installedTick;
    public TMP_Text label;

    UpgradeDef upgradeDef;
    UpgradeUI upgradeUI;

    public void Init(UpgradeUI upgradeUI, UpgradeDef upgradeDef)
    {
        this.upgradeDef = upgradeDef;
        this.upgradeUI = upgradeUI;

        this.UpdateState();
    }

    public void Install()
    {
        this.upgradeUI.Install(this.upgradeDef);
    }

    public void UpdateState()
    {
        this.installButton.interactable = this.upgradeUI.CanInstall(this.upgradeDef);
        this.installedTick.SetActive(this.upgradeUI.IsInstalled(this.upgradeDef));
        this.label.text = this.upgradeDef.name;
    }
}
