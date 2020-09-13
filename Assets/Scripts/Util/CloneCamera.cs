using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CloneCamera : MonoBehaviour
{
    // Update is called once per frame
    void LateUpdate()
    {
        var parentCamera = this.GetComponentInParentOnly<Camera>();
        var thisCamera = this.GetComponent<Camera>();

        thisCamera.orthographicSize = parentCamera.orthographicSize;
        thisCamera.nearClipPlane = parentCamera.nearClipPlane;
        thisCamera.farClipPlane = parentCamera.farClipPlane;
    }
}
