using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MissionBoardUI : MissionListUIBase
{
    public Button sellDataButton;
    public TMP_Text sellDataButtonText;
    
    protected override IEnumerable<IMissionBase> MissionList => this.missions.availableMissions;
    protected override GameObject CreateUI(IMissionFactory factory, IMissionBase mission, Transform parent) => factory.CreateBoardUI(this.missions, mission, parent);

    protected override void InitUI()
    {
        base.InitUI();
        
        this.sellDataButton.onClick.AddListener(() => {
            this.missions.SellNewData();
            this.UpdateUI();
        });
    }

    protected override void UpdateUI()
    {
        base.UpdateUI();
        this.missions.UpdateNewDataReward();
        this.sellDataButton.gameObject.SetActive(this.missions.NewDataReward > 0);
        this.sellDataButtonText.text = $"Sell Data for {this.missions.NewDataReward} cr";
    }
}
