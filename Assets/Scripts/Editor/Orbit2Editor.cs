using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Orbit2))]
public class Orbit2Editor : Editor
{
    void OnSceneGUI()
    {
        if (Application.isPlaying)
            return;

        var orbit = this.target as Orbit2;
        if (orbit.pathPositions == null)
        {
            return;
        }

        float uiScale = HandleUtility.GetHandleSize(orbit.transform.position) * 0.2f;

        Handles.Label(orbit.transform.position + (Vector3.right * 0.2f + Vector3.up) * orbit.parameters.distance * (1f + uiScale * 0.1f), $"{orbit.gameObject.name}", UnityEditor.EditorStyles.whiteLargeLabel);

        // Draw orbit
        Handles.matrix = orbit.transform.localToWorldMatrix;
        Handles.DrawAAPolyLine(orbit.pathPositions);
        Handles.DrawAAPolyLine(Vector3.zero, orbit.pathPositions[0]);

        // Draw handles
        var angleRot = Quaternion.Euler(0, 0, orbit.parameters.angle);
        bool DistAngleHandle()
        {
            Handles.color = Color.magenta;
            Handles.matrix = orbit.transform.localToWorldMatrix;

            var distAngleHandlePos = angleRot * (Vector3.right * (orbit.parameters.distance + 3f));
            Handles.DrawAAPolyLine(Vector3.zero, distAngleHandlePos);
            Handles.Label(distAngleHandlePos + Vector3.right * 2 * uiScale, $"distance: {orbit.parameters.distance:0.0}\nangle: {orbit.parameters.angle:0.0}°");
            EditorGUI.BeginChangeCheck();
            var newDistAngle = Handles.Slider2D(
                distAngleHandlePos,
                Vector3.forward,
                Vector3.right,
                Vector3.up,
                uiScale,
                Handles.CircleHandleCap,
                Vector2.zero);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(this.target, "Changed Orbit2 distance/angle");
                orbit.parameters.distance = newDistAngle.magnitude - 3f;
                orbit.parameters.angle = Vector3.SignedAngle(Vector3.right, newDistAngle, Vector3.forward);
                orbit.RefreshValidateRecursive();
                return true;
            }
            return false;
        };
        DistAngleHandle();

        bool AngularVelocityHandle()
        {
            Handles.color = Color.cyan;
            Handles.matrix = orbit.transform.localToWorldMatrix * Matrix4x4.Rotate(angleRot) * Matrix4x4.Translate(Vector3.right * orbit.parameters.distance);

            const float AngularVelocityScale = 3f;
            var handleDir = orbit.parameters.angularVelocity >= 0 ? Vector3.up : Vector3.down;
            var angularVelocityHandlePos = Vector3.up * orbit.parameters.angularVelocity * AngularVelocityScale; // angleRot * (Vector3.right * orbit.parameters.distance + Vector3.up * orbit.parameters.angularVelocity);
            Handles.Label(angularVelocityHandlePos + (Vector3.right + handleDir * 5f) * uiScale, $"angular velocity: {orbit.parameters.angularVelocity:0.00}°/s");
            Handles.DrawAAPolyLine(Vector3.zero, angularVelocityHandlePos);
            
            EditorGUI.BeginChangeCheck();
            var newSliderPos = Handles.Slider(
                angularVelocityHandlePos,
                handleDir,
                5f * uiScale,
                Handles.ArrowHandleCap,
                0);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(this.target, "Changed Orbit2 angular velocity");
                orbit.parameters.angularVelocity = newSliderPos.y / AngularVelocityScale;
                orbit.RefreshValidateRecursive();
                return true;
            }
            return false;
        };
        AngularVelocityHandle();
    }

    [DrawGizmo(GizmoType.NotInSelectionHierarchy)]
    static void RenderCustomGizmo(Transform objectTransform, GizmoType gizmoType)
    {
        // Draw orbit
        Handles.color = new Color(0.33f, 0.33f, 0.33f);
        foreach (var orbit in FindObjectsOfType<Orbit>())
        {
            Handles.matrix = orbit.transform.localToWorldMatrix;
            Handles.DrawPolyLine(orbit.editorPath);
            Handles.DrawPolyLine(Vector3.zero, orbit.editorPath[0]);
        }
    }
}