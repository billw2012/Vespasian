using UnityEngine;
using UnityEngine.UI;

public class MapUI : MonoBehaviour, IUILayer
{
    public RectTransform mapView;
    public Button jumpButton;
    public GameObject systemMarkerPrefab;
    public GameObject linkMarkerPrefab;

    public GameLogic gameLogic;

    private MapComponent mapComponent;

    public MissionMapUI missionMapUi;

    // Start is called before the first frame update
    private void Start()
    {
        this.mapComponent = ComponentCache.FindObjectOfType<MapComponent>();
        this.CreateMap();
        this.mapComponent.mapGenerated.AddListener(this.CreateMap);
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
        foreach (Transform ob in this.mapView.transform)
        {
            Destroy(ob.gameObject);
        }

        if (this.mapComponent.map != null)
        {
            foreach (var s in this.mapComponent.map.links)
            {
                var inst = ComponentCache.Instantiate(this.linkMarkerPrefab, this.mapView);
                var instScript = inst.GetComponent<MapLinkMarkerUI>();
                instScript.link = s;
                instScript.mapComponent = this.mapComponent;
            }

            // Prepare an array of system refs for which we have active missions
            var missions = ComponentCache.FindObjectOfType<Missions>();
            //var systemMissions = missions.activeMissions.Where(mn => mn is MissionSurveySystem && !mn.IsComplete);
            //var systemRefs = systemMissions.Select(mn => (mn as MissionSurveySystem).targetSystemRef.systemId);

            foreach (var s in this.mapComponent.map.systems)
            {
                var inst = ComponentCache.Instantiate(this.systemMarkerPrefab, this.mapView);
                var instScript = inst.GetComponent<MapSystemMarkerUI>();
                instScript.system = s;
                instScript.mapComponent = this.mapComponent;
                instScript.mapUi = this;
            }
        }
    }

    private void Update() => this.jumpButton.interactable = this.mapComponent.CanJump();

    #region IUILayer
    public void OnAdded() {}
    // When the Map is hidden, reset the selected system
    public void OnRemoved() => this.mapComponent.selectedSystem = null;
    public void OnDemoted() {}
    public void OnPromoted() {}
    #endregion IUILayer
}
