using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class GalaxyUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    [SerializeField]
    GalaxyCameraController camController;

    [SerializeField]
    Camera mainCamera;

    [SerializeField]
    GalaxyShapePreview galaxy;

    [SerializeField]
    Canvas canvas;

    [SerializeField]
    RectTransform nameMarkerParentTransform;

    [SerializeField]
    float maxSelectionDistance = 0.1f;

    [SerializeField]
    GameObject solarSystemNameMarkerPrefab;

    public float mouseSensitivity = 0.1f;

    public float clipSystemMarkers = 0.3f;

    private float camAngleStartRad;
    private Vector2 pointerPosStartDrag;
    private bool dragging = false;

    private bool debug = false;

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
        float deltaRad = - deltax * this.mouseSensitivity;
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
        Ray rayFromCamera = this.mainCamera.ScreenPointToRay(eventData.position);
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
            SolarSystem closestSystem = this.galaxy.map.FindNearestSystem(pos2D, this.maxSelectionDistance);
            if (closestSystem != null)
            {
                if (debug) Debug.Log($"  Closest system: {closestSystem} at { closestSystem.position}");
                this.galaxy.SelectSolarSystem(closestSystem);
            }
        }
        else
        {
            if (debug) Debug.Log("  No intersection");
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

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

        Matrix4x4 viewProjectionMatrix = this.mainCamera.projectionMatrix * this.mainCamera.worldToCameraMatrix;

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
                var systemPosCanvas = this.canvas.WorldToCanvasPosition(systemPosUi3d);
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

        foreach (var solarSystem in this.galaxy.map.systems)
        {
            SystemUiInfo uiInfo = new SystemUiInfo();
            uiInfo.system = solarSystem;
            uiInfo.nameMarker = GameObject.Instantiate(this.solarSystemNameMarkerPrefab, this.nameMarkerParentTransform);
            uiInfo.selected = false;
            this.systemUiInfo.Add(solarSystem, uiInfo);

            TextMeshProUGUI tmp = uiInfo.nameMarker.GetComponentInChildren<TextMeshProUGUI>();
            tmp.text = solarSystem.name;
        }
    }
}
