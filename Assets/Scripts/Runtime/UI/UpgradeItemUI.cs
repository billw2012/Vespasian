using TMPro;
using UnityEngine;
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
        this.costLabel.text = isInstalled
            ? ""
            : canAfford
                ? $"<style=credits>{this.upgradeDef.cost} cr"
                : $"<style=credits-bad>{this.upgradeDef.cost} cr";
    }
}
