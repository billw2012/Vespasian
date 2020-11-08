using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class FuelBar : MonoBehaviour
{
    public Slider mainSlider;
    public Slider usageSlider;

    PlayerController player;

    UpgradeComponentProxy<EngineComponent> engine;
    MapComponent map;

    void Start()
    {
        this.player = FindObjectOfType<PlayerController>();
        this.engine = this.player.GetComponent<UpgradeManager>().GetProxy<EngineComponent>();
        this.map = FindObjectOfType<MapComponent>();
    }

    void Update()
    {
        this.mainSlider.value = this.engine.value.fuel;
        this.usageSlider.value = 1 - Mathf.Clamp(this.map.GetJumpFuelRequired() / this.engine.value.fuel, 0, 1);
    }
}
