using UnityEngine;
using System.Linq;
using UnityEditor;
using System.Collections.Generic;

/*
 * This is the passive docking port, to be attached to space stations
 */

public class DockPassive : EffectSource
{
    [Tooltip("Transform of this spacecraft to be used for docking purposes")]
    public Transform spacecraftTransform;

    [Tooltip("Renders sprite with radius where we can dock from")]
    public SpriteRenderer dockingRadiusRenderer;

    [Tooltip("Must point to orbit object of this spacecraft if it's orbiting anything")]
    public Orbit orbit;

    // Start is called before the first frame update
    void Start()
    {
        this.dockingRadiusRenderer.transform.localScale = this.range * 2 * Vector3.one;
    }

    private void OnValidate()
    {
        this.dockingRadiusRenderer.transform.localScale = this.range * 2 * Vector3.one;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public override Color gizmoColor => Color.red;
    public override string debugName => "DockPassive";

    public void OnDrawGizmos()
    {
        float arrowLength = 0.5f;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(this.transform.position, 0.15f);
        Gizmos.DrawLine(this.transform.TransformPoint(new Vector3(0, arrowLength, 0)),
            this.transform.TransformPoint(Vector3.zero));
    }
}
