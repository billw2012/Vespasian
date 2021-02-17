using UnityEngine;
using UnityEngine.EventSystems;

/*
 * Transforms player dragging the screen into thrust controls for player ship.
 * Must be attached to UI.
 */

[RequireComponent(typeof(RectTransform))]
public class PlayerMovementControllerDragJoystick : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private PlayerController controller;
    private Vector2 posStart;
    private Vector2 posLast;

    [SerializeField, Tooltip("Dead zone, 0..1")]
    private float deadZone = 0.25f;

    [SerializeField, Tooltip("Offset value corresponding to max input value")]
    private float maxOffset = 200;

    [SerializeField, Tooltip("This is required to disable thrust when we are using the two-finger pinch zoom")]
    private PinchZoomCamera pinchZoomCamera = null; // todo find a better way to solve this

    private void Awake()
    {
        this.controller = ComponentCache.FindObjectOfType<PlayerController>();
    }

    private void UpdateThrust()
    {
        // Special check for the pinch zoom camera
        if (this.pinchZoomCamera != null)
        {
            if (this.pinchZoomCamera.Pinching)
            {
                this.controller.thrustInputJoystick = Vector2.zero;
                return;
            }
        }

        Vector2 offset = this.posLast - this.posStart;

        offset /= this.maxOffset;

        // Apply dead zone
        if (Mathf.Abs(offset.x) < this.deadZone)
            offset.x = 0;
        if (Mathf.Abs(offset.y) < this.deadZone)
            offset.y = 0;

        // Clamp inputs to -1...1
        offset.x = Mathf.Clamp(offset.x, -1, 1);
        offset.y = Mathf.Clamp(offset.y, -1, 1);

        this.controller.thrustInputJoystick = offset;
    }

    void IBeginDragHandler.OnBeginDrag(PointerEventData eventData)
    {
        //Debug.Log("DragJoystick: begin drag");
        this.posStart = eventData.position;
        this.posLast = this.posStart;
        this.UpdateThrust();
    }

    void IDragHandler.OnDrag(PointerEventData eventData)
    {
        //Debug.Log("DragJoystick: on drag");
        this.posLast = eventData.position;
        this.UpdateThrust();
    }

    void IEndDragHandler.OnEndDrag(PointerEventData eventData)
    {
        this.posStart = Vector2.zero;
        this.posLast = Vector2.zero;
        this.UpdateThrust();
    }
}
