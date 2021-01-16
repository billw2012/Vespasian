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
    public Vector3 targetVelocity { get; set; }

    private void Update()
    {
        var currentVelocity = this.GetComponent<SimMovement>().velocity;
        var globalThrustVector = this.targetVelocity - currentVelocity;
        this.SetThrustGlobal(globalThrustVector);
        Debug.DrawLine(this.transform.position, this.transform.position + this.targetVelocity, Color.yellow);
        Debug.DrawLine(this.transform.position, this.transform.position + globalThrustVector, Color.magenta);
    }
}