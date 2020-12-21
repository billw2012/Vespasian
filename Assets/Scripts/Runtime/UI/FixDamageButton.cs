// unset

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FixDamageButton : MonoBehaviour
{
    public Image buttonImage;
    public Button button;
    public TMP_Text buttonText;
    public float fixCostPerHP = 100f;
    
    private PlayerController player;
    private float repairCost;

    private void Awake()
    {
        this.player = FindObjectOfType<PlayerController>();
        this.button.onClick.AddListener(() =>
        {
            var health = this.player.GetComponent<HealthComponent>();
            health.FullyRepairHull();
        });
    }

    private void Update()
    {
        var health = this.player.GetComponent<HealthComponent>();
        if(health.isDamaged)
        {
            this.repairCost = health.damagedHP * this.fixCostPerHP;
            this.button.enabled = this.buttonImage.enabled = true;
            this.buttonText.text = $"Repair <style=hp>{health.damagedHP}</style> hp for <style=credits>{this.repairCost} cr</style>";
        }
        else
        {
            this.repairCost = 0;
            this.button.enabled = this.buttonImage.enabled = false;
            this.buttonText.text = $"No repairs needed";
        }
    }
}