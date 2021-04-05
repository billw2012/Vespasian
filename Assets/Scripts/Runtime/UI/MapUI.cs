using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class MapUI : MonoBehaviour, IUILayer, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{

    // ---- Handles to external components ----

    public RectTransform mapView;
    public Button jumpButton;

    // Prefabs for the UI
    public GameObject starIconPrefab = null;
    public GameObject starLinkPrefab = null;
    public GameObject solarSystemNameMarkerPrefab;

    // Transforms
    public Transform map3dObjectsRoot = null;       // Transform for 3D UI objects
    public Transform systemNameMarkersRoot;    // Transform for UI markers

    // Selector for the currently selected galaxy
    public GameObject systemSelector = null;

    public GameLogic gameLogic = null;

    private MapComponent mapComponent = null;

    public MissionMapUI missionMapUi = null;

    public GalaxyCameraController camController = null;

    public Camera galaxyCamera = null;  // Camera used for galaxy UI
    public Camera mainCamera = null;    // Main camera used by the game

    // ---- Other fields ----

    private float camAngleStartRad;
    private Vector2 pointerPosStartDrag;
    private bool dragging = false;

    // Some settings
    public float mouseSensitivity = 0.003f;     // For scrolling speed
    public float clipSystemMarkers = 0.5f;      // Max distance for rendering markers
    public float maxSelectionDistance = 0.1f;   // For selection


    // Set to true to dump some dat into the log
    public bool debug = false;

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
        this.UnvisualizeAllSystems();

        if (this.mapComponent.map != null)
        {
            this.VisualizeAllSystems();

            /*
            foreach (var s in this.mapComponent.map.links)
            {
                var inst = ComponentCache.Instantiate(this.linkMarkerPrefab, this.mapView);
                var instScript = inst.GetComponent<MapLinkMarkerUI>();
                instScript.link = s;
                instScript.mapComponent = this.mapComponent;
            }
            */

            // Prepare an array of system refs for which we have active missions
            var missions = ComponentCache.FindObjectOfType<Missions>();
            //var systemMissions = missions.activeMissions.Where(mn => mn is MissionSurveySystem && !mn.IsComplete);
            //var systemRefs = systemMissions.Select(mn => (mn as MissionSurveySystem).targetSystemRef.systemId);

            /*
            foreach (var s in this.mapComponent.map.systems)
            {
                var inst = ComponentCache.Instantiate(this.systemMarkerPrefab, this.mapView);
                var instScript = inst.GetComponent<MapSystemMarkerUI>();
                instScript.system = s;
                instScript.mapComponent = this.mapComponent;
                instScript.mapUi = this;
            }
            */
        }
    }

    void UnvisualizeAllSystems()
    {
        foreach (Transform ob in this.map3dObjectsRoot)
        {
            Destroy(ob.gameObject);
        }
    }

    // Just puts stars to the scene for visualization
    void VisualizeAllSystems()
    {
        // Galaxy map is rendered on its own layer
        int layerId = LayerMask.NameToLayer("GalaxyMap");
        Debug.Assert(layerId != -1);

        // Place system markers
        foreach (var sys in this.mapComponent.map.systems)
        {
            Vector2 pos2d = sys.position;
            Vector3 pos3d = new Vector3(pos2d.x, 0, pos2d.y);
            GameObject starObj = GameObject.Instantiate(this.starIconPrefab, this.map3dObjectsRoot);
            starObj.layer = layerId;
            starObj.transform.localPosition = pos3d;
            float starSize = 0.013f;
            starObj.transform.localScale = new Vector3(starSize, starSize, starSize);
        }

        // Place link markers
        foreach (var link in this.mapComponent.map.links)
        {
            GameObject linkObj = GameObject.Instantiate(this.starLinkPrefab, this.map3dObjectsRoot);
            linkObj.layer = layerId;
            LineRenderer line = linkObj.GetComponent<LineRenderer>();
            Vector3[] positions =
            {
                GalaxyMapMath.Vec2dTo3d(this.mapComponent.map.GetSystem(link.from).position),
                GalaxyMapMath.Vec2dTo3d(this.mapComponent.map.GetSystem(link.to).position)
            };
            line.SetPositions(positions);
        }
    }

    private void Update() => this.jumpButton.interactable = this.mapComponent.CanJump();


    public void OnBeginDrag(PointerEventData eventData)
    {
        if (debug) Debug.Log("Galaxy drag begin");

        this.camAngleStartRad = Mathf.Deg2Rad * this.camController.cameraAngleDeg;
        this.pointerPosStartDrag = eventData.position;
        this.dragging = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        //Debug.Log("Galaxy drag");
        Vector2 pointerPosDelta = eventData.position - this.pointerPosStartDrag;
        float deltax = pointerPosDelta.x;
        float deltaRad = -deltax * this.mouseSensitivity;
        float angleRad = this.camAngleStartRad + deltaRad;
        this.camController.SetCameraAngle(angleRad);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (debug) Debug.Log("Galaxy drag end");

        this.dragging = false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Bail if dragging
        // This event handler is called when we release the pointer while dragging
        if (dragging)
            return;
        if (debug) Debug.Log($"Galaxy click: {eventData.position}");
        Ray rayFromCamera = this.galaxyCamera.ScreenPointToRay(eventData.position);
        if (debug) Debug.Log($"  Ray from camera: origin: {rayFromCamera.origin}, dir: {rayFromCamera.direction}");
        Plane galaxyPlane = new Plane(new Vector3(0, 1, 0), Vector3.zero);
        float rayIntersectionDist;
        bool hasIntersected = galaxyPlane.Raycast(rayFromCamera, out rayIntersectionDist);
        if (hasIntersected)
        {
            if (debug) Debug.Log($"  Ray intersection dist: {rayIntersectionDist}");
            Vector3 intersectionPos = rayFromCamera.GetPoint(rayIntersectionDist);
            if (debug) Debug.Log($"  Ray intersection pos: {intersectionPos}");
            Vector2 pos2D = GalaxyMapMath.Vec3dTo2d(intersectionPos);
            SolarSystem closestSystem = this.mapComponent.map.FindNearestSystem(pos2D, this.maxSelectionDistance);
            if (closestSystem != null)
            {
                if (debug) Debug.Log($"  Closest system: {closestSystem} at { closestSystem.position}");
                // todo handle galaxy selection here
                //this.galaxy.SelectSolarSystem(closestSystem);
            }
        }
        else
        {
            if (debug) Debug.Log("  No intersection");
        }
    }


    // ---- 2D markers for systems ----

    struct SystemUiInfo
    {
        public SolarSystem system;
        public GameObject nameMarker;
        public bool selected;
        // ??
    }

    bool markersInitialized = false;
    Dictionary<SolarSystem, SystemUiInfo> systemUiInfo;

    void LateUpdate()
    {
        if (!this.markersInitialized)
        {
            // Cheap way to make it run after Awake() of galaxy shape preview
            this.InitSystemNameMarkers();
            this.markersInitialized = true;
        }

        Matrix4x4 viewProjectionMatrix = this.galaxyCamera.projectionMatrix * this.galaxyCamera.worldToCameraMatrix;

        // Update positions of all markers
        foreach (var keyValue in this.systemUiInfo)
        {
            SolarSystem solarSystem = keyValue.Key;
            var systemPosUi3d = GalaxyMapMath.Vec2dTo3d(solarSystem.position);
            Vector3 projectedPos = viewProjectionMatrix.MultiplyPoint(systemPosUi3d);

            SystemUiInfo uiInfo = keyValue.Value;
            TextMeshProUGUI tmp = uiInfo.nameMarker.GetComponentInChildren<TextMeshProUGUI>();
            RectTransform markerTransform = uiInfo.nameMarker.GetComponent<RectTransform>();
            if (projectedPos.z < this.clipSystemMarkers)
            {
                Canvas canvas = this.GetComponentInParent<Canvas>();
                var systemPosCanvas = canvas.WorldToCanvasPosition(systemPosUi3d);
                markerTransform.anchoredPosition = systemPosCanvas;
                tmp.enabled = true;
            }
            else
            {
                tmp.enabled = false;
            }
        }
    }

    void InitSystemNameMarkers()
    {
        this.systemUiInfo = new Dictionary<SolarSystem, SystemUiInfo>();

        foreach (var solarSystem in this.mapComponent.map.systems)
        {
            SystemUiInfo uiInfo = new SystemUiInfo();
            uiInfo.system = solarSystem;
            uiInfo.nameMarker = GameObject.Instantiate(this.solarSystemNameMarkerPrefab, this.systemNameMarkersRoot);
            uiInfo.selected = false;
            this.systemUiInfo.Add(solarSystem, uiInfo);

            TextMeshProUGUI tmp = uiInfo.nameMarker.GetComponentInChildren<TextMeshProUGUI>();
            tmp.text = solarSystem.name;
        }
    }




    #region IUILayer
    public void OnAdded()
    {
        this.mainCamera.enabled = false;
        this.galaxyCamera.enabled = true;
    }
    // When the Map is hidden, reset the selected system
    public void OnRemoved()
    {
        this.mapComponent.selectedSystem = null;
        this.mainCamera.enabled = true;
        this.galaxyCamera.enabled = false;
    }
    public void OnDemoted() {}
    public void OnPromoted() {}
    #endregion IUILayer
}
