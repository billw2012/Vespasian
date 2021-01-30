using UnityEngine;

/*
 * This is the passive docking port, to be attached to space stations
 */

public class DockPassive : EffectSource
{
    [Tooltip("Transform of this spacecraft to be used for docking purposes")]
    public Transform spacecraftTransform;

    [Tooltip("Must point to orbit object of this spacecraft if it's orbiting anything")]
    public Orbit orbit;

    public override Color gizmoColor => Color.red;
    public override string debugName => "DockPassive";

    protected override void Awake()
    {
        base.Awake();
        this.alwaysRevealed = true;
    }

    public void OnDrawGizmos()
    {
        float arrowLength = 0.5f;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(this.transform.position, 0.15f);
        Gizmos.DrawLine(this.transform.TransformPoint(new Vector3(0, arrowLength, 0)),
            this.transform.TransformPoint(Vector3.zero));
    }
}
