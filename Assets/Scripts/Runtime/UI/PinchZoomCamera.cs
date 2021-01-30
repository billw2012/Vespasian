using UnityEngine;

public class PinchZoomCamera : MonoBehaviour
{
    // Camera size (zoom) limits
    [Tooltip("Minimum view size"), Range(5, 40)]
    public float sizeMin = 10f;
    [Tooltip("Maximum view size"), Range(40, 1000)]
    public float sizeMax = 100f;
    [Tooltip("Scroll wheel sensitivity"), Range(0.01f, 3f)]
    public float scrollWheelSensitivity = 1f;

    private bool pinching = false;
    private float distStart = 0;    // Distance between fingers when we started pinching
    private float distCurrent = 0;  // Current distance between fingers

    private float targetSize; // Target camera size
    private float camSizeStart = 1;  // Camera size when we started pinching

    public Camera cameraComponent;

    // Start is called before the first frame update
    private void Start()
    {
        this.targetSize = this.cameraComponent.orthographicSize;
    }

    // Update is called once per frame
    private void Update()
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
