using System.Collections;
using System.Collections.Generic;
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

    public Transform target;

    public LineRenderer trackedPath;

    Vector2 offset;
    Vector2 offsetVelocity;
    float initialCameraSize;

    void Start()
    {
        Assert.IsNotNull(this.target);
        Assert.IsNotNull(this.trackedPath);

        this.initialCameraSize = Camera.main.orthographicSize;
    }

    Vector2 ClampToCameraInnerArea(Vector2 vec)
    {
        var center = this.transform.position;
        var tr = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width * 0.8f, Screen.height * 0.8f, this.transform.position.z));
        var cameraArea = new Rect(center - tr, (tr - center) * 2);
        var maxOffset = cameraArea.IntersectionWithRayFromCenter(vec);
        return Vector3.ClampMagnitude(vec, maxOffset.magnitude);
    }

    void Update()
    {
        var targetOffset = (Vector2)this.trackedPath.bounds.center - (Vector2)this.target.position;

        float cameraZoom = this.initialCameraSize / Camera.main.orthographicSize;
        var smoothedOffset = Vector2.SmoothDamp(this.offset, targetOffset, ref this.offsetVelocity, this.smoothTime * cameraZoom);

        var clampedOffset = this.ClampToCameraInnerArea(smoothedOffset);

        // Don't modify the z coordinate
        this.transform.position = (this.target.transform.position + (Vector3)clampedOffset).xy0() + this.transform.position._00z();
        this.offset = clampedOffset;
    }
}
