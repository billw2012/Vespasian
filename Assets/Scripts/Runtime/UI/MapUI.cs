using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

public class MapUI : MonoBehaviour, IUILayer
{
    public RectTransform mapView;
    public Button jumpButton;
    public GameObject systemMarkerPrefab;
    public GameObject linkMarkerPrefab;

    private MapComponent mapComponent;

    // Start is called before the first frame update
    private void Start()
    {
        this.mapComponent = FindObjectOfType<MapComponent>();
        this.CreateMap();
    }

    private void OnEnable()
    {
        foreach (var linkMarker in this.mapView.GetComponentsInChildren<MapLinkMarkerUI>())
        {
            linkMarker.Refresh();
        }
        foreach (var systemMarker in this.mapView.GetComponentsInChildren<MapSystemMarkerUI>())
        {
            systemMarker.Refresh();
        }
    }

    private void CreateMap()
    {
        foreach (var s in this.mapComponent.map.links)
        {
            var inst = Instantiate(this.linkMarkerPrefab, this.mapView);
            var instScript = inst.GetComponent<MapLinkMarkerUI>();
            instScript.link = s;
            instScript.mapComponent = this.mapComponent;
        }

        foreach (var s in this.mapComponent.map.systems)
        {
            var inst = Instantiate(this.systemMarkerPrefab, this.mapView);
            var instScript = inst.GetComponent<MapSystemMarkerUI>();
            instScript.system = s;
            instScript.mapComponent = this.mapComponent;
        }
    }

    private void Update()
    {
        this.jumpButton.interactable = this.mapComponent.CanJump();
    }

    #region IUILayer
    public void OnAdded() {}
    // When the Map is hidden, reset the selected system
    public void OnRemoved() => this.mapComponent.selectedSystem = null;
    public void OnDemoted() {}
    public void OnPromoted() {}
    #endregion IUILayer
}
