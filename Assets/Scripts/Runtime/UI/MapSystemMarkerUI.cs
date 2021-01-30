using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MapSystemMarkerUI : MonoBehaviour
{
    public MapComponent mapComponent;
    public MapUI mapUi;
    public SolarSystem system;
    public Color color = Color.gray;
    public Color currentColor = Color.white;
    public Color jumpTargetColor = Color.green;
    public GameObject currentMarker;
    public GameObject selectMarker;
    public GameObject stationMarker;
    public GameObject missionMarker;
    public Image starImage;

    public TextMeshProUGUI label;

    public bool isCurrent => this.system == this.mapComponent.currentSystem;
    public bool isSelected => this.system == this.mapComponent.selectedSystem;
    public bool isJumpTarget => this.system == this.mapComponent.jumpTarget;
    public bool isValidJumpTarget => this.mapComponent.GetValidJumpTargets().Contains(this.system);

    private bool missionMarkerEnabled
    {
        set => this.missionMarker.SetActive(value);
    }
    
    private bool currentMarkerEnabled
    {
        set => this.currentMarker.SetActive(value);
    }
    
    private bool stationMarkerEnabled
    {
        set => this.stationMarker.SetActive(value);
    }
    
    private bool selectMarkerEnabled
    {
        set => this.selectMarker.SetActive(value);
    }
    
    private void Awake()
    {
        this.label.enabled = false;
        this.currentMarkerEnabled = false;
        this.missionMarkerEnabled = false;
        this.stationMarkerEnabled = false;
        this.selectMarkerEnabled = false;
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

        this.currentMarkerEnabled = this.isCurrent;
        this.selectMarkerEnabled = this.isSelected;
    }

    public void Clicked()
    {
        this.mapComponent.selectedSystem = this.system;
        this.mapComponent.TrySetJumpTarget(this.system);

        this.mapUi.missionMapUi.UpdateMissionList(this.system.id);
    }

    public void Refresh()
    {
        this.stationMarkerEnabled = this.system.AllBodies().OfType<Station>().Any();
        this.missionMarkerEnabled =
            this.mapUi.missionMapUi.missions != null &&
            this.mapUi.missionMapUi.missions.HasActiveMissionsInSystem(this.system.id);
    }
}
