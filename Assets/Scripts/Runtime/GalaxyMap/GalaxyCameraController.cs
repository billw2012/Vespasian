﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GalaxyCameraController : MonoBehaviour
{
    [Range(-360*3, 0)]
    public float cameraAngleDeg = 90;

    public GalaxyShapePreview shapePreview = null;

    public Transform mapViewTransform = null;  // Transform of whole map view

    [Range(0, 1)]
    public float cameraOffsetDistance = 0.3f;

    [Range(0, 1)]
    public float cameraOffsetHeight = 0.3f;

    [Range(0, 1)]
    public float startCamOffsetRatio = 0.3f;

    public float cameraAngleLimitMin = -600;
    public float cameraAngleLimitMax = 0;

    // Cursor calculations
    Vector3 prevCursorPos;
    public float mouseSensitivity = 1;


    private void UpdateCameraPos(float angleRad)
    {
        var shape = this.shapePreview.GetGalaxyShape();
        var camPositions = shape.CameraPositions(angleRad, this.cameraOffsetDistance, this.cameraOffsetHeight, this.startCamOffsetRatio, 1.0f);
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

    /*
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
    */

    public void SetCameraAngle(float angleRad)
    {
        this.cameraAngleDeg = Mathf.Clamp(Mathf.Rad2Deg * angleRad, cameraAngleLimitMin, cameraAngleLimitMax);
        this.UpdateCameraPos(Mathf.Deg2Rad * cameraAngleDeg);
        //Debug.Log($"Camera angle: {this.cameraAngleDeg}");
    }
}
