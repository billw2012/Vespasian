using UnityEditor;
using UnityEngine;

public abstract class RingEffectSource : EffectSource
{
    [Tooltip("Radius of the middle of the ring"), Range(0, 50)]
    public float radius = 10.0f;

    public override float GetDistance(Transform other)
    {
        return Mathf.Abs(this.originTransform.worldToLocalMatrix.MultiplyPoint(other.position).xy0().magnitude - this.radius);
    }

#if UNITY_EDITOR
    private new void OnDrawGizmosSelected()
    {
        // Display the explosion radius when selected
        Handles.color = this.gizmoColor;
        Handles.matrix = this.originTransform.localToWorldMatrix;
        Handles.DrawWireDisc(Vector3.zero, Vector3.forward, this.radius - this.range);
        Handles.DrawWireDisc(Vector3.zero, Vector3.forward, this.radius + this.range);
        GUIUtils.Label(Quaternion.Euler(0, 0, this.debugName.GetHashCode() % 90) * Vector2.left * this.radius, this.debugName);
    }
#endif
}
