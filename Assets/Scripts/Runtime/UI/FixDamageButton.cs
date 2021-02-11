using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FixDamageButton : MonoBehaviour
{
    [SerializeField]
    private float fixCostPerHP = 100f;
    
    private PlayerController player;
    private int repairCost;
    private float repairHP;
    private Missions missions;

    private Image buttonImage;
    private Button button;
    private TMP_Text buttonText;

    private void Awake()
    {
        this.buttonImage = this.GetComponentInChildren<Image>();
        this.button = this.GetComponentInChildren<Button>();
        this.buttonText = this.GetComponentInChildren<TMP_Text>();

        this.player = ComponentCache.FindObjectOfType<PlayerController>();
        this.missions = ComponentCache.FindObjectOfType<Missions>();
        this.button.onClick.AddListener(() =>
        {
            this.player.GetComponent<HealthComponent>().AddHull(this.repairHP);
            this.missions.SubtractFunds(this.repairCost);
            NotificationsUI.Add($"Hull repaired");
        });
    }

    private void Update()
    {
        var health = this.player.GetComponent<HealthComponent>();
        if(health.isDamaged)
        {
            this.repairCost = Mathf.Min(this.missions.playerCredits, Mathf.CeilToInt(health.damagedHP * this.fixCostPerHP));
            this.repairHP = Mathf.Min(health.damagedHP, this.repairCost / this.fixCostPerHP);
            this.buttonImage.enabled = true;
            this.button.enabled = this.missions.playerCredits > 0;
            this.buttonText.text = this.repairHP == health.damagedHP 
                ? $"Repair <style=hp>{this.repairHP}</style> hp for <style=credits>{this.repairCost} cr</style>" 
                : $"Repair <style=hp>{this.repairHP}</style> of {health.damagedHP} hp for <style=credits>{this.repairCost} cr</style>";
        }
        else
        {
            this.repairCost = 0;
            this.repairHP = 0;
            this.button.enabled = this.buttonImage.enabled = false;
            this.buttonText.text = $"No repairs needed";
        }
    }
}