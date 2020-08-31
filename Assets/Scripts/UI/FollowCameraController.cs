using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

/*
 * How to do follow camera with context focus:
 * - Use bounds of section of sim path (from the start) that will fit on screen at maximum position
 * - When there is specific targets / scoring opportunities then add them to the context bounding box?
 * - 
 */
public class FollowCameraController : MonoBehaviour
{
    [Tooltip("How fast the camera can move to its desired offset"), Range(0f, 10f)]
    public float smoothTime = 3f;

    [Tooltip("How much space to keep within the target and the screen edge"), Range(0f, 10f)]
    public float margin = 2f;

    public Transform target;

    public SimManager simManager;

    Vector2 offset;
    Vector2 offsetVelocity;
    float initialCameraSize;

    CameraPointOfInterest[] scenePointsOfInterest;

    void Start()
    {
        Assert.IsNotNull(this.target);
        Assert.IsNotNull(this.simManager);

        this.initialCameraSize = Camera.main.orthographicSize;

        this.scenePointsOfInterest = GameObject.FindObjectsOfType<CameraPointOfInterest>();
    }

    Vector2 ClampToCameraInnerArea(Vector2 vec)
    {
        var center = this.transform.position;
        var tr = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width * 0.8f, Screen.height * 0.8f, this.transform.position.z));
        var cameraArea = new Rect(center - tr, (tr - center) * 2);
        var maxOffset = cameraArea.IntersectionWithRayFromCenter(vec);
        return Vector3.ClampMagnitude(vec, maxOffset.magnitude);
    }

    static Rect WorldCameraArea()
    {
        var center = Camera.main.transform.position;
        var tr = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width * 0.8f, Screen.height * 0.8f, center.z));
        return new Rect(center - tr, (tr - center) * 2);
    }

    //Vector3 GetTrackedPathFocusPoint()
    //{
    //    // focus formula:
    //    // 

    //}

    // Used locally for sorting
    struct PointOfInterest
    {
        public Vector3 position;
        public Vector3 fullSize;
        public float distance; // For sorting
        public PointOfInterest(Vector3 pos, Vector3 size, float dist)
        {
            this.position = pos;
            this.fullSize = size;
            this.distance = dist;
        }
    }

    void Update()
    {
        // Combine spheres of influence from sim path and points of interest from scene
        // Both use distance metric, but distance for SOIs is measured along the simulated path
        // and distance for basic POIs is measured from player
        //var pointsOfInterest = this.simManager.sois
        //    .Select(i => new PointOfInterest(i.g.position, i.g.transform.localScale, i.distance))
        //    .Concat(this.scenePointsOfInterest.Select(i => new PointOfInterest(i.transform.position, i.size, Vector3.Distance(i.transform.position, this.target.position))))
        //    .OrderBy(i => i.distance); // Sort by ascending distance

        // Determining which pois to use:
        // keep adding pois until their bounding box exceeds the camera inner area available

        var cameraArea = WorldCameraArea();
        var bounds = new Bounds((Vector2)this.target.position, Vector2.one * this.margin);
        //foreach (var poi in pointsOfInterest)
        //{
        //    var expandedBounds = bounds;
        //    expandedBounds.Encapsulate(new Bounds((Vector2)poi.position, (Vector2)poi.fullSize));
        //    if (expandedBounds.size.magnitude > cameraArea.size.magnitude)
        //    {
        //        break;
        //    }
        //    bounds = expandedBounds;
        //}

        var targetOffset = (Vector2)bounds.center - (Vector2)this.target.position;

        float cameraZoom = this.initialCameraSize / Camera.main.orthographicSize;
        var smoothedOffset = Vector2.SmoothDamp(this.offset, targetOffset, ref this.offsetVelocity, this.smoothTime * cameraZoom);

        var clampedOffset = this.ClampToCameraInnerArea(smoothedOffset);

        // Don't modify the z coordinate
        this.transform.position = (this.target.transform.position + (Vector3)clampedOffset).xy0() + this.transform.position._00z();
        this.offset = clampedOffset;
    }
}
