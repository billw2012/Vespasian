using IngameDebugConsole;
using System;
using UnityEngine;

/// <summary>
/// Represents the players control of some in game entity (typically a ship).
/// Specifically:
/// - converts player input into thrust applied to the EngineController
/// - provides simplified interface to common player ship functions
/// - registers debug console functions for player
/// </summary>
public class AIController : MonoBehaviour, ISavable, ISavableCustom
{
    public Vector3 targetVelocity { get; set; }

    private void Update()
    {
        // var engine = this.GetComponent<EngineController>();
        //
        // // Convert normalized inputs into final values in (kind of) Newtons
        // engine.thrust.y = engine.constants.ThrustForward * Mathf.Clamp(this.thrustInputForward + this.thrustInputJoystick.y + kbInput.y, -1, 1);
        // engine.thrust.x = engine.constants.ThrustRight * Mathf.Clamp(this.thrustInputRight + this.thrustInputJoystick.x + kbInput.x, -1, 1); ;
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

    [RegisterSavableType(typeof(Vector3)), RegisterSavableType(typeof(Quaternion))]
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