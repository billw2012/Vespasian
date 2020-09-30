using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DragJoystick : MonoBehaviour
{
    PlayerController playerLogic;
    Vector2 posStart;
    bool dragging = false;

    const float deadZone = 0.25f;
    const float JoystickSizePx = 200;

    void Start()
    {
        this.playerLogic = FindObjectOfType<PlayerController>();
    }

    void Update()
    {
        if (Input.touchCount == 1)
        {
            var touch = Input.GetTouch(0);
            if (!this.dragging)
            {
                this.posStart = touch.position;
                this.dragging = true;
            }
            var offset = Vector2.ClampMagnitude(touch.position - this.posStart, JoystickSizePx) / DragJoystick.JoystickSizePx;

            // Apply dead zone
            if (Mathf.Abs(offset.x) < DragJoystick.deadZone)
                offset.x = 0;
            if (Mathf.Abs(offset.y) < DragJoystick.deadZone)
                offset.y = 0;

            playerLogic.thrustInputJoystick = offset;
        }
        else
        {
            if (this.dragging)
            {
                this.dragging = false;
            }
            playerLogic.thrustInputJoystick = Vector2.zero;
        }
    }
}
