using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DragJoystick : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler {

    public GameObject player = null;
    Vector2 dragStart;

    // Output values, -1 ... +1
    private float outx = 0;
    private float outy = 0;

    // Start is called before the first frame update
    void Start()
    {
        Debug.Assert(this.player != null);
    }

    // Update is called once per frame
    void Update()
    {
        var playerLogic = this.player.GetComponent<PlayerLogic>();

        float debugx = this.outx * 100;
        float debugy = this.outy * 100;
        Debug.Log($"Joystick output: {debugx}, {debugy}");

        // Bail if we are not flying yet
        if (playerLogic.state == PlayerLogic.e_state.flying) {
            playerLogic.thrustForward = outy;
            playerLogic.thrustRight = outx;
        }
    }

    public void OnBeginDrag(PointerEventData eventData) {

        var playerLogic = this.player.GetComponent<PlayerLogic>();

        this.dragStart = eventData.position;
    }

    public void OnDrag(PointerEventData eventData) {

        float joystickSizePx = 200;
        Vector2 offset = Vector2.ClampMagnitude(eventData.position - this.dragStart, joystickSizePx) / joystickSizePx;
        this.outx = offset.x;
        this.outy = offset.y;
    }

    public void OnEndDrag(PointerEventData eventData) {
        outx = 0;
        outy = 0;
    }
}
