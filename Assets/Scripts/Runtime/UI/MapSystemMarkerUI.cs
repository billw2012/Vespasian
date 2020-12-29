﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MapSystemMarkerUI : MonoBehaviour
{
    public MapComponent mapComponent;
    public SolarSystem system;
    public Color color = Color.gray;
    public Color currentColor = Color.white;
    public Color jumpTargetColor = Color.green;
    public GameObject currentMarker;
    public GameObject selectMarker;
    public GameObject stationMarker;
    public Image starImage;

    public TextMeshProUGUI label;

    public bool isCurrent => this.system == this.mapComponent.currentSystem;
    public bool isSelected => this.system == this.mapComponent.selectedSystem;
    public bool isJumpTarget => this.system == this.mapComponent.jumpTarget;
    public bool isValidJumpTarget => this.mapComponent.GetValidJumpTargets().Contains(this.system);

    private void Awake()
    {
        this.label.enabled = false;
        this.currentMarker.SetActive(false);
    }

    private void Start()
    {
        var instTransform = this.transform as RectTransform;
        instTransform.anchorMin = this.system.position;
        instTransform.anchorMax = this.system.position;
        this.label.text = this.system.name;
        this.Refresh();
    }

    private void Update()
    {
        this.starImage.color = this.isCurrent
            ? this.currentColor
            : this.isJumpTarget
            ? this.jumpTargetColor
            : this.color;

        this.currentMarker.SetActive(this.isCurrent);
        this.selectMarker.SetActive(this.isSelected);
    }

    public void Clicked()
    {
        this.mapComponent.selectedSystem = this.system;
        this.mapComponent.TrySetJumpTarget(this.system);
    }

    public void Refresh() => this.stationMarker.SetActive(this.system.AllBodies().OfType<Station>().Any());
}
