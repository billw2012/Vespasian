using IngameDebugConsole;
using UnityEngine;

/// <summary>
/// Toggles the UI and the in-game debug console with a single key.
/// Attach this to the UIRoot. Disables the Canvas component rather than the
/// GameObject so this script keeps running and can toggle back on.
/// </summary>
[RequireComponent(typeof(Canvas))]
public class UIToggle : MonoBehaviour
{
    public KeyCode toggleKey = KeyCode.F1;
    public KeyCode godModeKey = KeyCode.F2;
    public KeyCode giveCreditsKey = KeyCode.F3;
    public KeyCode revealMapKey = KeyCode.F4;

    private Canvas canvas;
    private bool uiVisible = true;

    private void Awake()
    {
        this.canvas = this.GetComponent<Canvas>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(this.toggleKey))
        {
            this.uiVisible = !this.uiVisible;

            this.canvas.enabled = this.uiVisible;

            var console = DebugLogManager.Instance;
            if (console != null)
            {
                if (this.uiVisible)
                {
                    console.PopupEnabled = true;
                }
                else
                {
                    console.HideLogWindow();
                    console.PopupEnabled = false;
                }
            }
        }

        if (Input.GetKeyDown(this.godModeKey))
        {
            HealthComponent.DebugTogglePlayerGodMode();
        }

        if (Input.GetKeyDown(this.giveCreditsKey))
        {
            Missions.DebugPlayerGiveCredits(100000);
        }

        if (Input.GetKeyDown(this.revealMapKey))
        {
            Missions.DebugPlayerRevealMap();
        }

        // Hold number keys to speed up simulation (matching UI buttons: x4, x16, x32)
        int speedStep = 1;
        if      (Input.GetKey(KeyCode.Alpha2)) speedStep = 4;
        else if (Input.GetKey(KeyCode.Alpha3)) speedStep = 16;
        else if (Input.GetKey(KeyCode.Alpha4)) speedStep = 32;
        Simulation.SetGlobalTickStep(speedStep);
    }
}
