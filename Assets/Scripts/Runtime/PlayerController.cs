using System;
using UnityEngine;

[RequireComponent(typeof(SimMovement))]
public class PlayerController : MonoBehaviour
{
    [NonSerialized]
    // NORMALIZED dimensionless thrust input for joystick
    // x is -1...1 <=> +right/-left, y is -1...1 <=> +forward/-backward
    public Vector2 thrustInputJoystick = Vector2.zero;
    // NORMALIZED dimensionless thrust input for separate axis -1..1 <=> -max...+max
    public float thrustInputForward { get; set; }
    public float thrustInputRight { get; set; }

    void Awake()
    {
        this.thrustInputJoystick = Vector2.zero;
    }

    void Update()
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

        var engine = this.GetComponent<EngineComponent>();

        // Convert normalized inputs into final values in (kind of) Newtons
        engine.thrust.y = engine.constants.ThrustForward * Mathf.Clamp(this.thrustInputForward + this.thrustInputJoystick.y + kbInput.y, -1, 1);
        engine.thrust.x = engine.constants.ThrustRight * Mathf.Clamp(this.thrustInputRight + this.thrustInputJoystick.x + kbInput.x, -1, 1); ;
    }

    public void SetAllowDamageAndCollision(bool allow)
    {
        foreach (var collider in this.GetComponentsInChildren<Collider>())
        {
            collider.enabled = allow;
        }
        this.GetComponent<HealthComponent>().allowDamage = allow;
    }
}