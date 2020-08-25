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

        Handles.Label(orbit.transform.position + (Vector3.right * 0.2f + Vector3.up) * orbit.parameters.semiMajorAxis * 0.5f, $"{orbit.gameObject.name}", UnityEditor.EditorStyles.whiteLargeLabel);

        // Draw orbit
        Handles.matrix = orbit.transform.localToWorldMatrix;
        Handles.DrawAAPolyLine(orbit.pathPositions);
        Handles.DrawAAPolyLine(Vector3.zero, orbit.pathPositions[0]);

        // Draw handles
        var angleRot = Quaternion.Euler(0, 0, orbit.parameters.angle);
        bool PeriapsisAngleHandle()
        {
            Handles.color = Color.magenta;
            Handles.matrix = orbit.transform.localToWorldMatrix;

            var handlePos = angleRot * (Vector3.right * (orbit.parameters.periapsis + 3f));
            Handles.DrawAAPolyLine(Vector3.zero, handlePos);
            Handles.Label(handlePos + Vector3.right * 2 * uiScale, $"periapsis: {orbit.parameters.periapsis:0.0}\nangle: {orbit.parameters.angle:0.0}°");
            EditorGUI.BeginChangeCheck();
            var newValue = Handles.Slider2D(
                handlePos,
                Vector3.forward,
                Vector3.right,
                Vector3.up,
                uiScale,
                Handles.CircleHandleCap,
                Vector2.zero);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(this.target, "Changed Orbit2 periapsis/angle");
                float previousRatio = orbit.parameters.periapsis / orbit.parameters.periapsis;
                orbit.parameters.SetPeriapsisMaintainEccentricity(Mathf.Max(0, newValue.magnitude - 3f));
               // orbit.parameters.apoapsis = orbit.parameters.periapsis * previousRatio;
                orbit.parameters.angle = Vector3.SignedAngle(Vector3.right, newValue, Vector3.forward);
                orbit.RefreshValidateRecursive();
                return true;
            }
            return false;
        };
        PeriapsisAngleHandle();

        bool ApoapsisHandle()
        {
            Handles.color = Color.magenta;
            Handles.matrix = orbit.transform.localToWorldMatrix;

            var handlePos = angleRot * (Vector3.left * (orbit.parameters.apoapsis + 3f));
            Handles.DrawAAPolyLine(Vector3.zero, handlePos);
            Handles.Label(handlePos + Vector3.right * 2 * uiScale, $"apoapsis: {orbit.parameters.apoapsis:0.0}\neccentricity: {orbit.parameters.eccentricity}");
            EditorGUI.BeginChangeCheck();
            var newValue = Handles.Slider2D(
                handlePos,
                Vector3.forward,
                Vector3.left,
                Vector3.up,
                uiScale * 0.5f,
                Handles.CircleHandleCap,
                0);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(this.target, "Changed Orbit2 apoapsis");
                orbit.parameters.apoapsis = Mathf.Max(orbit.parameters.periapsis, newValue.magnitude - 3f);
                orbit.RefreshValidateRecursive();
                return true;
            }
            return false;
        };
        ApoapsisHandle();
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