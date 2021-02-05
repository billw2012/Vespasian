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

    [SerializeField]
    LineRenderer fireDirectionLineRenderer;

    // Update is called once per frame
    void Update()
    {
        var currentWeapon = this.shipController.GetWeaponController().GetCurrentWeapon();
        if (dragJoystick.userInputActive)
        {
            this.fireDirectionLineRenderer.enabled = true;
            Vector2 joyInput = this.dragJoystick.userInputValue;
            float inputLen = joyInput.magnitude;
            if (inputLen > 0.9f)
            {
                if (currentWeapon != null)
                {
                    currentWeapon.FireAt(joyInput);
                }
            }

            // Handle the fire direction line renderer
            Vector3 posStart = this.shipController.transform.position;
            Vector3 posEnd = posStart + (Vector3)joyInput.normalized * 100.0f;
            Vector3[] linePositions =
            {
                posStart,
                posEnd
            };
            linePositions[0].z = 0;
            linePositions[1].z = 0;
            this.fireDirectionLineRenderer.SetPositions(linePositions);
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
            if (currentWeapon.Heat > 0)
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
