using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GalaxyCameraController : MonoBehaviour
{
    [Range(-360*3, 360)]
    public float cameraAngleDeg = 90;

    public GalaxyShapePreview shapePreview = null;

    public Transform mapViewTransform = null;  // Transform of whole map view

    [Range(0, 1)]
    public float cameraOffsetDistance = 0.3f;

    [Range(0, 1)]
    public float cameraOffsetHeight = 0.3f;

    // Cursor calculations
    Vector3 prevCursorPos;
    public float mouseSensitivity = 1;




    private void UpdateCameraPos(float angleRad)
    {
        var shape = this.shapePreview.GetGalaxyShape();
        var camPositions = shape.CameraPositions(angleRad, this.cameraOffsetDistance, this.cameraOffsetHeight);
        this.transform.localPosition = camPositions.Item1;
        this.transform.LookAt(this.mapViewTransform.position + camPositions.Item2);
    }

    private void OnValidate()
    {
        // Get properties of galaxy shape
        if (this.shapePreview == null)
            return;

        this.UpdateCameraPos(Mathf.Deg2Rad * this.cameraAngleDeg);
    }

    void Update()
    {
        Vector3 cursorPos = Input.mousePosition;

        if (Input.GetMouseButton(0))
        {
            Vector3 deltaMovement = cursorPos - prevCursorPos;
            float xMovement = deltaMovement.x;
            this.cameraAngleDeg -= this.mouseSensitivity * xMovement;
            //Debug.Log($"Current camera angle: {this.cameraAngleDeg} degrees");

            // Update camera pos
            this.UpdateCameraPos(Mathf.Deg2Rad * this.cameraAngleDeg);
        }

        this.prevCursorPos = cursorPos;
    }
}
