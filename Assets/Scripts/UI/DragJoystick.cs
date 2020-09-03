using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DragJoystick : MonoBehaviour
{
    PlayerLogic playerLogic;
    Vector2 posStart;
    bool dragging = false;

    void Start()
    {
        this.playerLogic = FindObjectOfType<PlayerLogic>();
    }

    void Update()
    {
        Debug.Log($"Touch count: {Input.touchCount}");
        if (Input.touchCount == 1)
        {
            var touch = Input.GetTouch(0);
            if (!this.dragging)
            {
                this.posStart = touch.position;
                this.dragging = true;
            }
            const float JoystickSizePx = 200;
            var offset = Vector2.ClampMagnitude(touch.position - this.posStart, JoystickSizePx) / JoystickSizePx;
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
