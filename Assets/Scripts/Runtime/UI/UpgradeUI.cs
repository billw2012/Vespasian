using IngameDebugConsole;
using UnityEngine;

public class UpgradeUI : MonoBehaviour
{
    public GameObject grid;
    public GameObject defaultUpgradeUIPrefab;

    private UpgradeManager upgradeManager;
    private Missions missions;

    private void Awake()
    {
        DebugLogConsole.AddCommand( "show-upgrade-menu", "Shows the upgrade menu", () => ComponentCache.FindObjectOfType<GUILayerManager>().PushLayer(this.gameObject) );
    }

    private void OnEnable()
    {
        this.upgradeManager = ComponentCache.FindObjectOfType<PlayerController>().GetComponent<UpgradeManager>();
        this.missions = ComponentCache.FindObjectOfType<Missions>();
        this.UpdateGrid();
    }

    private void UpdateGrid()
    {
        foreach(Transform upgradeUI in this.grid.transform)
        {
            Destroy(upgradeUI.gameObject);
        }

        foreach(var upgradeDef in this.upgradeManager.fullUpgradeSet.upgradesDefs)
        {
            var upgradeUI = ComponentCache.Instantiate(upgradeDef.shopUIPrefab == null? this.defaultUpgradeUIPrefab : upgradeDef.shopUIPrefab, this.grid.transform);
            var upgradeItemUI = upgradeUI.GetComponent<UpgradeItemUI>();
            upgradeItemUI.Init(this, upgradeDef, this.missions);
        }
    }

    public void Install(UpgradeDef upgradeDef)
    {
        this.upgradeManager.Upgrade(upgradeDef, testFire: true);
        this.missions.SubtractFunds(upgradeDef.cost);
        this.UpdateGrid();
    }

    public bool CanInstall(UpgradeDef upgradeDef) => this.upgradeManager.CanInstall(upgradeDef) || this.upgradeManager.CanLevelUp(upgradeDef);
    public bool IsInstalled(UpgradeDef upgradeDef) => this.upgradeManager.IsInstalled(upgradeDef);
}
