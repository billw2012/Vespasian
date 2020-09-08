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

    [Tooltip("What the camera should follow (required)")]
    public Transform target;

    [Tooltip("The SimMomement component of the target for SOI focus (optional)")]
    public SimMovement simMovement;

    [Tooltip("Camera will try to include points of interest into the view")]
    public bool searchPointsOfInterest = true;

    [Tooltip("Camera position will always be clamped to include target into the view")]
    public bool clampToCameraInnerArea = true;

    Vector2 currentOffset;
    Vector2 offsetVelocity; // Modified by SmoothDamp on each update
    Vector2 smoothedOffset; // Smoothed offset value without clamping
    float initialCameraSize;


    CameraPointOfInterest[] scenePointsOfInterest;

    void Start()
    {
        Assert.IsNotNull(this.target);

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

    void Update()
    {
        var bounds = new Bounds((Vector2)this.target.position, Vector2.one * this.margin);

        // Include other points of interested only if this is enabled
        if (this.searchPointsOfInterest)
        {
            // Combine spheres of influence from sim path and points of interest from scene
            // Both use distance metric, but distance for SOIs is measured along the simulated path
            // and distance for basic POIs is measured from player
            var pointsOfInterest = this.scenePointsOfInterest.Select(i => (
                    position: i.transform.position,
                    size: i.size,
                    distance: Vector3.Distance(i.transform.position, this.target.position)
                 ));
            if (this.simMovement != null)
            {
                pointsOfInterest = pointsOfInterest.Concat(
                    this.simMovement.sois.Select(i => (
                        position: i.g.position,
                        size: i.g.transform.localScale,
                        distance: Vector3.Distance(i.g.position, this.target.position)
                    )));
            }

            // Determining which pois to use:
            // keep adding pois until their bounding box exceeds the camera inner area available

            var cameraArea = WorldCameraArea();
            foreach (var poi in pointsOfInterest.OrderBy(i => i.distance))
            {
                var expandedBounds = bounds;
                expandedBounds.Encapsulate(new Bounds((Vector2)poi.position, (Vector2)poi.size));
                if (expandedBounds.size.magnitude > cameraArea.size.magnitude)
                {
                    break;
                }
                bounds = expandedBounds;
            }
        }

        var offsetToTarget = (Vector2)bounds.center - (Vector2)this.target.position;

        float cameraZoom = this.initialCameraSize / Camera.main.orthographicSize;
        var smoothedOffset = Vector2.SmoothDamp(this.currentOffset, offsetToTarget, ref this.offsetVelocity, this.smoothTime * cameraZoom);
        this.smoothedOffset = smoothedOffset;

        var clampedOffset = this.clampToCameraInnerArea ? this.ClampToCameraInnerArea(smoothedOffset) : smoothedOffset;

        // Don't modify the z coordinate
        this.transform.position = (this.target.transform.position + (Vector3)clampedOffset).xy0() + this.transform.position._00z();
        this.currentOffset = clampedOffset;
    }

    public void ForceFocusOnTarget()
    {
        if (this.target)
        {
            Vector3 targetPos = this.target.transform.position;
            this.transform.position = new Vector3(targetPos.x, targetPos.y, this.transform.position.z);
            this.currentOffset = Vector2.zero;
        }
        this.GetComponent<Camera>().orthographicSize = 12;
    }

    // Sets new target, use this for target changes at runtime to animate to another target
    public void SetTarget(Transform newTarget)
    {
        this.target = newTarget;
        this.currentOffset = this.transform.position - newTarget.transform.position;
        this.smoothedOffset = this.currentOffset;
        this.offsetVelocity = Vector2.zero;
    }

    // Returns true when camera has (sort of) arrived to the target position
    public bool atTargetPosition
    {
        get
        {
            float dist = Vector2.Distance(this.transform.position, this.target.transform.position);
            return (dist < 2.0f);
        }
    }
}
