using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/*
 * This component reads input from a drag joystick and controls the weapons of player's ship.
 * It doesn't matter much where to attach it. 
 */
public class PlayerWeaponController : MonoBehaviour
{
    [SerializeField]
    private DragJoystick dragJoystick = null;

    [SerializeField]
    private ControllerBase shipController = null;

    [SerializeField]
    private TextMeshProUGUI weaponPanelText = null;

    [SerializeField]
    private LineRenderer fireDirectionLineRenderer = null;

    // Update is called once per frame
    private void Update()
    {
        var currentWeapon = this.shipController.GetWeaponController().GetCurrentWeapon();
        if (dragJoystick.userInputActive)
        {
            this.fireDirectionLineRenderer.enabled = true;
            var joyInput = this.dragJoystick.userInputValue;
            float inputLen = joyInput.magnitude;
            if (inputLen > 0.9f)
            {
                if (currentWeapon != null)
                {
                    currentWeapon.FireAt(joyInput);
                }
            }

            // Handle the fire direction line renderer
            var posStart = this.shipController.transform.position;
            var posEnd = posStart + (Vector3)joyInput.normalized * 100.0f;
            this.fireDirectionLineRenderer.SetPositions(new []{
                posStart.xy0(),
                posEnd.xy0()
            });
        }
        else
        {
            this.fireDirectionLineRenderer.enabled = false;
        }

        // Update the weapon info panel
        if (currentWeapon == null)
        {
            this.weaponPanelText.text = "No weapon";
        }
        else
        {
            string text = $"Weapon: {currentWeapon.upgradeDef.name}";
            if (currentWeapon.Reloading)
                text = text + "\nReloading...!";
            //if (currentWeapon.Heat > 0)
            text = text + $"\nHeat: {currentWeapon.Heat}";
            if (currentWeapon.Overheat)
                text = text + "\nOverheat...!";
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
