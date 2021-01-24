using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class FuelGauge : MonoBehaviour
{
    [SerializeField]
    private GameObject fuelTankUIPrefab = null;

    private List<FuelTankBar> tankUI = new List<FuelTankBar>();

    private EngineController engineController;

    private void Start()
    {
        this.engineController = FindObjectOfType<PlayerController>().GetComponent<EngineController>();
    }

    private async void Update()
    {
        var tanks = this.engineController.allTanks;
        
        // Remove ui for any no-longer existing tanks
        var uisToRemove = this.tankUI
            .Where(u => tanks
                .All(t => u.fuelTank != t)).ToList();
        foreach (var uiToRemove in uisToRemove)
        {
            uiToRemove.gameObject.SetActive(false);
            Destroy(uiToRemove.gameObject);
            this.tankUI.Remove(uiToRemove);
        }

        // Add ui for any new tanks
        var tanksToAdd = tanks
            .Where(t => this.tankUI
                .All(u => u.fuelTank != t)).ToList();
        foreach (var tank in tanksToAdd)
        {
            var newUI = Instantiate(this.fuelTankUIPrefab, this.transform);
            var ui = newUI.GetComponent<FuelTankBar>();
            ui.fuelTank = tank;
            this.tankUI.Add(ui);
        }

        if (uisToRemove.Any() || tanksToAdd.Any())
        {
            await Awaiters.NextFrame; 
            // Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)this.transform);
        }
    }
}
