#if ENABLE_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM_PACKAGE
#define USE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

using UnityEngine;

[RequireComponent(typeof(Camera))]
public class SimpleCameraController : MonoBehaviour
{
    public float boost = 3.5f;
    //[Tooltip("Minimum view size"), Range(5, 40)]
    //public float sizeMin = 10f;
    //[Tooltip("Maximum view size"), Range(40, 200)]
    //public float sizeMax = 100f;
    [Tooltip("Scroll wheel sensitivity"), Range(0.01f, 3f)]
    public float scrollWheelSensitivity = 1f;


    private Vector3 GetInputTranslationDirection()
    {
        Vector3 direction = new Vector3();
        if (Input.GetKey(KeyCode.W))
        {
            direction += Vector3.up;
        }
        if (Input.GetKey(KeyCode.S))
        {
            direction += Vector3.down;
        }
        if (Input.GetKey(KeyCode.A))
        {
            direction += Vector3.left;
        }
        if (Input.GetKey(KeyCode.D))
        {
            direction += Vector3.right;
        }
        return direction;
    }

    private Vector3 startPos = Vector3.zero;
    private Vector2 startMousePos = Vector2.zero;
    private bool dragging = false;

    private void Update()
    {

        // Exit Sample  
        if (Input.GetKey(KeyCode.Escape))
        {
            Application.Quit();
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false; 
            #endif
        }

        // Hide and lock cursor when right mouse button pressed
        if (Input.GetMouseButtonDown(1))
        {
            this.dragging = true;
            this.startPos = this.transform.position;
            this.startMousePos = Input.mousePosition;
        }

        if (this.dragging)
        {
            // Unlock and show cursor when right mouse button released
            if (Input.GetMouseButtonUp(1))
            {
                this.dragging = false;
            }
            else
            {
                this.transform.position = this.startPos - 
                    (Camera.main.ScreenToWorldPoint(Input.mousePosition) - Camera.main.ScreenToWorldPoint(this.startMousePos)).xy0();
            }
        }
        else
        {
            // Translation
            Vector2 translation = this.GetInputTranslationDirection() * Time.deltaTime;

            // Speed up movement when shift key held
            if (Input.GetKey(KeyCode.LeftShift))
            {
                translation *= 10.0f;
            }

            // Modify movement by a boost factor (defined in Inspector and modified in play mode through the mouse scroll wheel)
            //boost += Input.mouseScrollDelta.y * 0.2f;
            translation *= Mathf.Pow(2.0f, this.boost);

            this.transform.position += (Vector3)translation;

            float size = this.GetComponent<Camera>().orthographicSize;

            this.GetComponent<Camera>().orthographicSize = Mathf.Max(size + -Input.GetAxis("Mouse ScrollWheel") * size * this.scrollWheelSensitivity, 0.1f);
        }
        
        // Framerate-independent interpolation
        // Calculate the lerp amount, such that we get 99% of the way to our target in the specified time
        //var positionLerpPct = 1f - Mathf.Exp((Mathf.Log(1f - 0.99f) / positionLerpTime) * Time.deltaTime);
        //var rotationLerpPct = 1f - Mathf.Exp((Mathf.Log(1f - 0.99f) / rotationLerpTime) * Time.deltaTime);
        //m_InterpolatingCameraState.LerpTowards(m_TargetCameraState, positionLerpPct, rotationLerpPct);

        //m_InterpolatingCameraState.UpdateTransform(transform);
    }
}
