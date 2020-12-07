using System;
using UnityEngine;

/// <summary>
/// Converts player input into thrust applied to the EngineController
/// </summary>
public class PlayerController : MonoBehaviour, ISavable, ISavableCustom
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
        FindObjectOfType<SaveSystem>().RegisterForSaving(this);
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

    #region ISavableCustom
    [RegisterSavableType(typeof(Vector3))]
    [RegisterSavableType(typeof(Quaternion))]
    public void Save(ISaver serializer)
    {
        var simComponent = this.GetComponent<SimMovement>();
        serializer.SaveValue("position", simComponent.transform.position);
        serializer.SaveValue("rotation", simComponent.transform.rotation);
        serializer.SaveValue("velocity", simComponent.velocity);
    }

    public void Load(ILoader deserializer)
    {
        var simComponent = this.GetComponent<SimMovement>();
        simComponent.SetPositionVelocity(
            deserializer.LoadValue<Vector3>("position"),
            deserializer.LoadValue<Quaternion>("rotation"),
            deserializer.LoadValue<Vector3>("velocity")
        );
    }
    #endregion ISavableCustom
}