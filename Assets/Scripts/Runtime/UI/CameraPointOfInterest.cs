using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/*
 * This component marks a point of interest for the camera.
 * FollowCameraController will search for these and try to keep them inside the camera view.
 * This is meant to be attached to objects without gravity sources, since gravity sources
 * are processed by the camera controller without this component.
 */
public class CameraPointOfInterest : MonoBehaviour
{
    // Point of interest can be a single point or a whole box for big objects
    public enum PointType
    {
        point,
        box
    }

    public PointType pointType = PointType.point;
    // Start is called before the first frame update
    /*
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
    */

    #if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (this.pointType == PointType.box)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position, transform.localScale);
            Handles.Label(transform.position, "Cam. Box Of Interest");
        }
    }
    #endif

    public Vector3 size
    {
        get {
            if (this.pointType == PointType.point)
                return new Vector3(0, 0, 0);
            else
                return this.transform.localScale;
        }
    }
}
