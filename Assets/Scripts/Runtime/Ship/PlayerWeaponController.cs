using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerWeaponController : MonoBehaviour
{
    [SerializeField]
    DragJoystick dragJoystick;

    [SerializeField]
    ControllerBase shipController;

    [SerializeField]
    TextMeshProUGUI weaponPanelText;

    // Update is called once per frame
    void Update()
    {
        var currentWeapon = this.shipController.GetWeaponController().GetCurrentWeapon();
        if (dragJoystick.userInputActive)
        {
            Vector2 joyInput = this.dragJoystick.userInputValue;
            float inputLen = joyInput.magnitude;
            if (inputLen > 0.5f)
            {
                if (currentWeapon != null)
                {
                    currentWeapon.FireAt(joyInput);
                }
            }
        }
        else
        {

        }

        // Update the weapon info panel
        if (currentWeapon == null)
        {
            this.weaponPanelText.text = "No weapon";
        }
        else
        {
            string text = $"Current weapon: {currentWeapon}";
            this.weaponPanelText.text = text;
        }
    }

    public void OnButtonCycleWeaponForward()
    {
        this.shipController.GetWeaponController().CycleCurrentWeapon(true);
    }

    public void OnButtonCycleWeaponBackward()
    {
        this.shipController.GetWeaponController().CycleCurrentWeapon(false);
    }
}
