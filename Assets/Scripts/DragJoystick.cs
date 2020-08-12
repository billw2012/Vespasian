using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DragJoystick : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler {

    public GameObject player;
    Vector2 dragStart;

    public void OnBeginDrag(PointerEventData eventData)
    {
        this.dragStart = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        const float JoystickSizePx = 200;

        var offset = Vector2.ClampMagnitude(eventData.position - this.dragStart, JoystickSizePx) / JoystickSizePx;

        var playerLogic = this.player.GetComponent<PlayerLogic>();
        playerLogic.manualThrust = offset;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        var playerLogic = this.player.GetComponent<PlayerLogic>();
        playerLogic.manualThrust = Vector2.zero;
    }
}
