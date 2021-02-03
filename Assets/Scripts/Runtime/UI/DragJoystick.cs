using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DragJoystick : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler
{
    [SerializeField]
    Canvas canvas;

    [SerializeField]
    RectTransform joystickRectTransform;

    [SerializeField]
    RectTransform knobRectTransform;

    Vector2 joystickSize;

    Vector2 offsetRelative;
    bool inputActive;
    Vector2 lastScreenPos;



    // Public interface for other components
    public Vector2 userInputValue { get => offsetRelative; }

    public bool userInputActive { get => this.inputActive; }

    // ----------------------------------



    void UpdateKnobPos(PointerEventData eventData)
    {
        Vector2 posScreen = eventData.position;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(this.joystickRectTransform, posScreen, null, out Vector2 knobPosLocal);

        float knobDistanceLocal = knobPosLocal.magnitude;
        float knobDistanceMax = 0.5f * this.joystickSize.x;
        this.offsetRelative = knobPosLocal / knobDistanceMax;
        if (knobDistanceLocal >= knobDistanceMax)
        {
            knobPosLocal = knobPosLocal.normalized * knobDistanceMax;
            this.offsetRelative = this.offsetRelative.normalized;
        }

        this.knobRectTransform.localPosition = knobPosLocal;

        //Debug.Log($"User input: {this.offsetRelative}");
    }

    void IBeginDragHandler.OnBeginDrag(PointerEventData eventData)
    {
        Debug.Log("OnBeginDrag");
        this.joystickSize = this.joystickRectTransform.offsetMax - this.joystickRectTransform.offsetMin;
        this.UpdateKnobPos(eventData);
        this.inputActive = true;
    }

    void IDragHandler.OnDrag(PointerEventData eventData)
    {
        this.UpdateKnobPos(eventData);        
    }

    void IEndDragHandler.OnEndDrag(PointerEventData eventData)
    {
        Debug.Log("OnEndDrag");
        this.knobRectTransform.localPosition = Vector2.zero;
        this.inputActive = false;
        this.offsetRelative = Vector2.zero;
    }
}
