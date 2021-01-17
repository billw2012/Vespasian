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
public class AIController : ControllerBase
{
    public Vector2 targetVelocity { get; set; }

    private void Update()
    {
        var currentVelocity = (Vector2)this.GetComponent<SimMovement>().velocity;
        var globalThrustVector = this.targetVelocity - currentVelocity;
        this.SetThrustGlobal(globalThrustVector);
        Debug.DrawLine(this.transform.position, this.transform.position + (Vector3)this.targetVelocity, Color.yellow);
        Debug.DrawLine(this.transform.position, this.transform.position + (Vector3)globalThrustVector, Color.magenta);
    }
}