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
    private bool targetVelocityEnabled;
    private Vector2 targetVelocity;

    public void SetTargetVelocity(Vector2 targetVelocity)
    {
        this.targetVelocityEnabled = true;
        this.targetVelocity = targetVelocity;
    }

    public void DisableTargetVelocity() => this.targetVelocityEnabled = false;

    private void Update()
    {
        if (this.targetVelocityEnabled)
        {
            var currentVelocity = (Vector2)this.GetComponent<SimMovement>().velocity;
            var globalThrustVector = this.targetVelocity - currentVelocity;
            this.SetThrustGlobal(globalThrustVector);
            Debug.DrawLine(this.transform.position, this.transform.position + (Vector3)this.targetVelocity, Color.yellow);
            Debug.DrawLine(this.transform.position, this.transform.position + (Vector3)globalThrustVector, Color.magenta);
        }
        else
        {
            this.SetThrustGlobal(Vector2.zero);
        }
    }
}