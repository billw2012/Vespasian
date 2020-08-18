using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PinchZoomCamera : MonoBehaviour
{
    // Camera size (zoom) limits
    public readonly float sizeMax = 40.0f;
    public readonly float sizeMin = 10.0f;

    bool pinching = false;
    float distStart = 0;    // Distance between fingers when we started pinching
    float distCurrent = 0;  // Current distance between fingers

    float targetSize; // Target camera size
    float camSizeStart = 1;  // Camera size when we started pinching

    Camera cameraComponent;

    // Start is called before the first frame update
    void Start()
    {
        this.cameraComponent = GetComponent<Camera>();
        this.targetSize = this.cameraComponent.orthographicSize;
    }


    // Update is called once per frame
    void Update()
    {
        // Handle screen touches.
        if (Input.touchCount == 2)
        {
            Touch[] touch = new Touch[2];
            touch[0] = Input.GetTouch(0);
            touch[1] = Input.GetTouch(1);
            float dist = Vector2.Distance(touch[0].position, touch[1].position);
            this.distCurrent = dist;

            if (!this.pinching) {
                this.distStart = dist;
                Debug.Log($"PinchZoomCamera: Start: distance: {this.distStart}");
                this.pinching = true;
            }

            float ratio = dist / this.distStart;
            Debug.Log($"PinchZoomCamera: ratio: {ratio}");
        }
        else
        {
            if (this.pinching)
            {
                this.pinching = false;
                Debug.Log($"PinchZoomCamera: end: distance: {this.distCurrent}");
                this.distCurrent = 0;
                this.distStart = 0;
            }
        }

        this.cameraComponent.orthographicSize = this.targetSize;
    }
}
