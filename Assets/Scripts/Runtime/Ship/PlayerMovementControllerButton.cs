using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/*
 * This component must be attached to a UI button, it will receive events and
 * convert them into commands for PlayerController
 */

[RequireComponent(typeof(Button))]
public class PlayerMovementControllerButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    private enum ButtonType
    {
        up,
        down,
        left,
        right
    }

    [SerializeField]
    private ButtonType buttonType = ButtonType.up;

    public void OnPointerDown(PointerEventData eventData)
    {
        var controller = ComponentCache.FindObjectOfType<PlayerController>();
        
        if (controller == null)
            return;

        switch (this.buttonType)
        {
            case ButtonType.up:
                controller.thrustInputForward = 1.0f;
                break;
            case ButtonType.down:
                controller.thrustInputForward = -1.0f;
                break;
            case ButtonType.right:
                controller.thrustInputRight = 1.0f;
                break;
            case ButtonType.left:
                controller.thrustInputRight = -1.0f;
                break;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        var controller = ComponentCache.FindObjectOfType<PlayerController>();

        if (controller == null)
            return;

        switch (this.buttonType)
        {
            case ButtonType.up:
            case ButtonType.down:
                controller.thrustInputForward = 0;
                break;
            case ButtonType.right:
            case ButtonType.left:
                controller.thrustInputRight = 0;
                break;
        }
    }
}
