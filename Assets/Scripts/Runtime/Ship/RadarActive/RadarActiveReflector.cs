using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * This component must be places on all objects which can be detected by the active radar.
 */

public class RadarActiveReflector : MonoBehaviour
{
    // We need these fields to obtain the speed of the object

    [SerializeField, Tooltip("Orbit component, if exists")]
    public Orbit orbit;

    [SerializeField, Tooltip("SimMovement component, if exists")]
    public SimMovement simMovement;

    public Vector3 Velocity
    {
        get
        {
            if (this.orbit != null)
                return orbit.absoluteVelocity;
            else if (this.simMovement != null)
                return simMovement.velocity;
            else
                return Vector3.zero;
        }
    }
}
