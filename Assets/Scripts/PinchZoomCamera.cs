using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PinchZoomCamera : MonoBehaviour
{
    // Camera size (zoom) limits
    [Tooltip("Minimum view size"), Range(5, 40)]
    public float sizeMin = 10f;
    [Tooltip("Maximum view size"), Range(40, 200)]
    public float sizeMax = 100f;
    [Tooltip("Scroll wheel sensitivity"), Range(0.01f, 3f)]
    public float scrollWheelSensitivity = 1f;

    bool pinching = false;
    float distStart = 0;    // Distance between fingers when we started pinching
    float distCurrent = 0;  // Current distance between fingers

    float targetSize; // Target camera size
    float camSizeStart = 1;  // Camera size when we started pinching


    Camera cameraComponent;

    // Start is called before the first frame update
    void Start()
    {
        this.cameraComponent = this.GetComponent<Camera>();
        this.targetSize = this.cameraComponent.orthographicSize;
    }


    // Update is called once per frame
    void Update()
    {
        // Handle screen touches.
        if (Input.touchCount == 2)
        {
            var touch = new Touch[2];
            touch[0] = Input.GetTouch(0);
            touch[1] = Input.GetTouch(1);
            float dist = Vector2.Distance(touch[0].position, touch[1].position);
            this.distCurrent = dist;

            if (!this.pinching) {
                this.distStart = dist;
                this.camSizeStart = this.cameraComponent.orthographicSize;
                Debug.Log($"PinchZoomCamera: Start: distance: {this.distStart}");
                this.pinching = true;
            }

            float ratio = dist / this.distStart;
            this.targetSize = this.camSizeStart / ratio;
            Debug.Log($"PinchZoomCamera: ratio: {ratio}");
        }
        else
        {
            if (this.pinching)
            {
                this.pinching = false;
                float ratio = this.distCurrent / this.distStart;
                Debug.Log($"PinchZoomCamera: end: distance: {this.distCurrent}, ratio: {ratio}");
            }
        }

        this.targetSize = Mathf.Clamp(this.targetSize + -Input.GetAxis("Mouse ScrollWheel") * this.targetSize * this.scrollWheelSensitivity, this.sizeMin, this.sizeMax);

        this.cameraComponent.orthographicSize = this.targetSize;
    }
}
