using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class GalaxySelector : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    [SerializeField]
    GalaxyCameraController camController;

    [SerializeField]
    Camera mainCamera;

    [SerializeField]
    GalaxyShapePreview galaxy;

    [SerializeField]
    float maxSelectionDistance = 0.1f;

    public float mouseSensitivity = 0.1f;

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
}
