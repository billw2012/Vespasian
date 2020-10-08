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

    bool docked = false;
    DockPassive passiveDockingPort = null; // Passive docking port component, if docked

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnDrawGizmos()
    {
        float arrowLength = 0.5f;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(this.dockingPortTransform.position, 0.15f);
        Gizmos.DrawLine(this.dockingPortTransform.TransformPoint(new Vector3(0, arrowLength, 0)),
            this.dockingPortTransform.TransformPoint(Vector3.zero));
    }

    // This port will try to dock to any passive port nearby
    public void ToggleDock()
    {
        Debug.Log("ToggleDock");
        if (this.docked)
        {
            // Undocking: unparent the transform, set our vleocity to velocity of space station
            Debug.Log("Currently docked");
            this.transform.SetParent(null); // Sets scene as parent
            this.EnableSimMovement(true);
            var simMovement = GetComponent<SimMovement>();
            var passiveOrbit = this.passiveDockingPort.orbit;
            if (simMovement != null)
            {
                if (passiveOrbit != null)
                {
                    simMovement.SetVelocity(passiveOrbit.velocity);
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
                this.EnableSimMovement(false);
                this.gameObject.transform.SetParent(passivePort.spacecraftTransform, true);
                Debug.Log($"Docked to {passivePort}");
                this.passiveDockingPort = passivePort;
                this.docked = true;
            }
            else
            {
                Debug.Log("No passive docking port nearby");
            }
        }
    }

    void EnableSimMovement(bool en)
    {
        var simMovement = GetComponent<SimMovement>();
        if (simMovement != null)
        {
            simMovement.enabled = en;
            Debug.Log("Disabled SimManager");
        }
    }
}
