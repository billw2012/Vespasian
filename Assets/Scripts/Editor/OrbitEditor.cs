using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Orbit))]
public class OrbitEditor : Editor
{
    void OnSceneGUI()
    {
        if (Application.isPlaying)
            return;

        var orbit = this.target as Orbit;

        if(!orbit.isActiveAndEnabled)
        {
            return;
        }

        float uiScale = HandleUtility.GetHandleSize(orbit.transform.position) * 0.2f;

        Handles.Label(orbit.transform.position + (Vector3.right * 0.2f + Vector3.up) * orbit.parameters.semiMajorAxis * 0.5f, $"{orbit.gameObject.name}", UnityEditor.EditorStyles.whiteLargeLabel);

        // Draw orbit
        Handles.matrix = orbit.transform.localToWorldMatrix;

        if (orbit.pathPositions != null)
        {
            Handles.DrawAAPolyLine(orbit.pathPositions);
            Handles.DrawAAPolyLine(orbit.pathPositions.Last(), orbit.pathPositions.First());
            Handles.DrawAAPolyLine(Vector3.zero, orbit.pathPositions.First());
        }

        // Draw handles
        var angleRot = Quaternion.Euler(0, 0, orbit.parameters.angle);
        bool PeriapsisAngleHandle()
        {
            Handles.color = Color.magenta;
            Handles.matrix = orbit.transform.localToWorldMatrix;

            var handlePos = angleRot * (Vector3.right * (orbit.parameters.periapsis + 3f));
            Handles.DrawAAPolyLine(Vector3.zero, handlePos);
            Handles.Label(handlePos + Vector3.right * 2 * uiScale, $"periapsis: {orbit.parameters.periapsis:0.0}\nangle: {orbit.parameters.angle:0.0}°\nshift = set eccentricity\nctrl = set angle");
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
                Undo.RecordObject(this.target, "Changed Orbit periapsis/angle");
                float previousRatio = orbit.parameters.periapsis / orbit.parameters.periapsis;
                float newPeriapsis = Mathf.Max(0, newValue.magnitude - 3f);
                if (Event.current.control)
                {
                    orbit.parameters.angle = Vector3.SignedAngle(Vector3.right, newValue, Vector3.forward);
                }
                else if (Event.current.shift)
                {
                    orbit.parameters.SetPeriapsis(newPeriapsis);
                }
                else
                {
                    orbit.parameters.SetPeriapsisMaintainEccentricity(newPeriapsis);
                }
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
            Handles.Label(handlePos + Vector3.right * 2 * uiScale, $"apoapsis: {orbit.parameters.apoapsis:0.0}\neccentricity: {orbit.parameters.eccentricity}\nctrl = set angle");
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
                Undo.RecordObject(this.target, "Changed Orbit apoapsis");
                if (Event.current.control)
                {
                    orbit.parameters.angle = Vector3.SignedAngle(Vector3.left, newValue, Vector3.forward);
                }
                else
                {
                    orbit.parameters.SetApoapsis(newValue.magnitude - 3f);
                }
                orbit.RefreshValidateRecursive();
                return true;
            }
            return false;
        };
        ApoapsisHandle();

        // Draw handles
        bool OffsetHandle()
        {
            Handles.color = Color.magenta;
            Handles.matrix = orbit.transform.localToWorldMatrix * Matrix4x4.Rotate(angleRot) * Matrix4x4.Translate(Vector3.right * (orbit.parameters.semiMajorAxis - orbit.parameters.apoapsis));

            var offsetRot = Quaternion.Euler(0, 0, orbit.parameters.offset * 360f);

            const float HandleOffset = 10f;
            var handlePos = offsetRot * (Vector3.right * (orbit.parameters.semiMajorAxis + HandleOffset));
            Handles.DrawAAPolyLine(Vector3.zero, handlePos);
            Handles.Label(handlePos + Vector3.right * 2 * uiScale, $"offset: {orbit.parameters.offset * 360f}°");
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
                Undo.RecordObject(this.target, "Changed Orbit apoapsis");
                orbit.parameters.offset = Vector3.SignedAngle(Vector3.left, newValue, Vector3.forward) / 360f + 0.5f;
                orbit.RefreshValidateRecursive();
                return true;
            }
            return false;
        };
        OffsetHandle();
    }

    static double lastUpdate = 0;

    [DrawGizmo(GizmoType.NotInSelectionHierarchy)]
    static void RenderCustomGizmo(Transform objectTransform, GizmoType gizmoType)
    {
        // Draw orbit
        Handles.color = new Color(0.33f, 0.33f, 0.33f);
        foreach (var orbit in FindObjectsOfType<Orbit>().Where(o => o.isActiveAndEnabled && o.pathPositions != null))
        {
            Handles.matrix = orbit.transform.localToWorldMatrix;
            Handles.DrawPolyLine(orbit.pathPositions);
            Handles.DrawPolyLine(orbit.pathPositions.Last(), orbit.pathPositions.First());
            Handles.DrawPolyLine(Vector3.zero, orbit.pathPositions.First());
        }

        if (EditorApplication.timeSinceStartup - lastUpdate > 0.1)
        {
            lastUpdate = EditorApplication.timeSinceStartup;
            var simModel = new SimModel();
            simModel.DelayedInit();
            foreach (var orbit in FindObjectsOfType<Orbit>())
            {
                orbit.RefreshValidateRecursive();
            }
        }
    }
}