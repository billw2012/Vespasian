using UnityEngine;

public class DragJoystick : MonoBehaviour
{
    private PlayerController playerLogic;
    private Vector2 posStart;
    private bool dragging = false;

    private const float deadZone = 0.25f;
    private const float JoystickSizePx = 200;

    private void Start()
    {
        this.playerLogic = FindObjectOfType<PlayerController>();
    }

    private void Update()
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

            this.playerLogic.thrustInputJoystick = offset;
        }
        else
        {
            if (this.dragging)
            {
                this.dragging = false;
            }
            this.playerLogic.thrustInputJoystick = Vector2.zero;
        }
    }
}
