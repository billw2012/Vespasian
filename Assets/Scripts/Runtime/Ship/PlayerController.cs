using System;
using UnityEngine;

/// <summary>
/// Represents the players control of some in game entity (typically a ship).
/// Specifically:
/// - converts player input into thrust applied to the EngineController
/// - provides simplified interface to common player ship functions
/// - registers debug console functions for player
/// </summary>
public class PlayerController : ControllerBase
{
    [NonSerialized]
    // NORMALIZED dimensionless thrust input for joystick
    // x is -1...1 <=> +right/-left, y is -1...1 <=> +forward/-backward
    public Vector2 thrustInputJoystick = Vector2.zero;
    // NORMALIZED dimensionless thrust input for separate axis -1..1 <=> -max...+max
    public float thrustInputForward { get; set; }
    public float thrustInputRight { get; set; }

    private void Awake()
    {
        this.thrustInputJoystick = Vector2.zero;
    }

    private void Update()
    {
        // Add thrust from keyboard
        var kbInput = new Vector2(0, 0);
        if (Input.GetKey("w"))
            kbInput.y = 1.0f;
        else if (Input.GetKey("s"))
            kbInput.y = -1.0f;

        if (Input.GetKey("d"))
            kbInput.x = 1.0f;
        else if (Input.GetKey("a"))
            kbInput.x = -1.0f;

        var engine = this.GetComponent<EngineController>();

        // Convert normalized inputs into final values in (kind of) Newtons
        this.SetThrust(
            this.thrustInputForward + this.thrustInputJoystick.y + kbInput.y, 
            this.thrustInputRight + this.thrustInputJoystick.x + kbInput.x
            );
    }
}