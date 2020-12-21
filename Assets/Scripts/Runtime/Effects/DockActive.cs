﻿using Pixelplacement;
using Pixelplacement.TweenSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * Must be attached to spacecraft itself. Not to the docking port.
 */

public class DockActive : MonoBehaviour
{
    [Tooltip("Transform of the docking port")]
    public Transform dockingPortTransform;

    public float refuelRate = 0.05f;
    
    public bool docked { get; private set; }

    private DockPassive passiveDockingPort = null; // Passive docking port component, if docked

    public void OnDrawGizmos()
    {
        float arrowLength = 0.5f;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(this.dockingPortTransform.position, 0.15f);
        Gizmos.DrawLine(this.dockingPortTransform.TransformPoint(new Vector3(0, arrowLength, 0)),
            this.dockingPortTransform.TransformPoint(Vector3.zero));
    }

    private readonly List<TweenBase> dockAnim = new List<TweenBase>();

    // This port will try to dock to any passive port nearby
    public void ToggleDock()
    {
        Debug.Log("ToggleDock");
        if (this.docked)
        {
            foreach(var da in this.dockAnim)
            {
                da.Stop();
            }
            this.dockAnim.Clear();

            // Undocking: unparent the transform, set our vleocity to velocity of space station
            Debug.Log("Currently docked");
            this.transform.SetParent(null); // Sets scene as parent
            this.EnableSimMovement(true);
            var simMovement = this.GetComponent<SimMovement>();
            var passiveOrbit = this.passiveDockingPort.orbit;
            if (simMovement != null)
            {
                if (passiveOrbit != null)
                {
                    simMovement.SetVelocity(passiveOrbit.absoluteVelocity);
                }
                else
                {
                    // What shall we do now?
                }
            }
            Debug.Log("Undocked");
            this.docked = false;
        }
        else
        {
            // Docking: disable our sim movement, set space station as our parent so we attach to it
            Debug.Log("Currently undocked");
            var passivePort = DockPassive.GetNearest<DockPassive>(this.transform);
            if (passivePort != null)
            {
                this.DockAt(passivePort);
            }
            else
            {
                Debug.Log("No passive docking port nearby");
            }
        }
    }

    public void DockAt(DockPassive passivePort)
    {
        this.EnableSimMovement(false);
        // Transform we will parent the ship to
        var dockParent = passivePort.transform.parent;
        this.gameObject.transform.SetParent(dockParent, worldPositionStays: true);

        // Relative transform from the parent to its docking port
        var dockTargetM = dockParent.worldToLocalMatrix * passivePort.transform.localToWorldMatrix;
        // Relative transform from the ship to its docking port
        var shipDockM = this.transform.worldToLocalMatrix * this.dockingPortTransform.transform.localToWorldMatrix;
        // Relative transform from the ship docking port, rotated 180 degrees, to the target docking port, in target coordinate system
        var relativeTransform = dockTargetM * Matrix4x4.Rotate(Quaternion.Euler(0, 0, 180)) * shipDockM.inverse;

        var targetPosition = new Vector3(relativeTransform.m03, relativeTransform.m13, relativeTransform.m23);

        //this.transform.localPosition = targetPosition;
        //this.transform.localRotation = relativeTransform.rotation;
        this.dockAnim.Add(Tween.LocalPosition(this.transform, targetPosition, 1, 0, Tween.EaseIn));
        this.dockAnim.Add(Tween.LocalRotation(this.transform, relativeTransform.rotation, 1, 0));

        Debug.Log($"Docked to {passivePort}");
        this.passiveDockingPort = passivePort;
        this.docked = true;
    }

    private void EnableSimMovement(bool en)
    {
        var playerController = this.GetComponent<PlayerController>();
        playerController.enabled = en;
        playerController.SetAllowDamageAndCollision(en);
        var playerSimMovement = this.GetComponent<SimMovement>();
        playerSimMovement.enabled = en;
    }

    private void Update()
    {
        if (this.docked)
        {
            this.GetComponentInChildren<EngineComponent>()?.AddFuel(Time.deltaTime * this.refuelRate);
        }
    }
}
