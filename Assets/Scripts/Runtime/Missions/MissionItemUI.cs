using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MissionItemUI : MonoBehaviour
{
    public TMP_Text nameText;
    public TMP_Text rewardText;
    public TMP_Text descriptionText;
    public Button actionButton;
    public TMP_Text actionButtonText;
    public GameObject completeMarker;

    private IMissionBase mission;
    private Missions missions;
    private bool activeMission;

    private DockActive playerDocking;
    
    public void Init(Missions missions, IMissionBase mission, bool activeMission)
    {
        this.mission = mission;
        this.missions = missions;
        this.activeMission = activeMission;

        this.playerDocking = FindObjectOfType<PlayerController>().GetComponentInChildren<DockActive>();

        this.nameText.text = mission.Name;
        this.descriptionText.text = mission.Description;
        this.rewardText.text = $"<style=credits>{mission.Reward} cr</style>";

        if (this.activeMission)
        {
            this.actionButtonText.text = "Hand In";
            this.actionButton.onClick.AddListener(() => missions.HandIn(mission));
        }
        else
        {
            this.actionButtonText.text = "Take";
            this.actionButton.onClick.AddListener(() => missions.Take(mission));
        }
    }

    private void Update()
    {
        if (this.activeMission)
        {
            this.actionButton.gameObject.SetActive(this.mission.IsComplete);
            this.actionButton.enabled = this.mission.IsComplete && this.playerDocking;
            this.completeMarker.SetActive(this.mission.IsComplete);
        }
    }
}
