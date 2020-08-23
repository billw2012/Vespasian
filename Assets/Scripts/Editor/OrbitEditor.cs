using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Orbit))]
public class OrbitEditor : Editor
{
    private static readonly int ControlIDBase = typeof(OrbitEditor).ToString().GetHashCode();

    void OnSceneGUI()
    {
        if (Application.isPlaying)
            return;

        var orbit = this.target as Orbit;

        float uiScale = HandleUtility.GetHandleSize(orbit.transform.position) * 0.2f;

        Handles.Label(orbit.transform.position + (Vector3.right * 0.2f + Vector3.up) * orbit.parameters.meanDistance * (1f + uiScale * 0.1f), $"{orbit.gameObject.name}", UnityEditor.EditorStyles.whiteLargeLabel);

        // Draw orbit
        Handles.matrix = orbit.transform.localToWorldMatrix;
        Handles.DrawAAPolyLine(orbit.editorPath);
        Handles.DrawAAPolyLine(Vector3.zero, orbit.editorPath[0]);

        // Draw movement per 10 seconds
        Handles.matrix = orbit.transform.localToWorldMatrix;
        Handles.color = new Color(1, 1, 1, 0.35f);
        var movementPositions = orbit.GetPositions(orbit.parameters.motionPerSecond * 10, 0.2f);
        if (movementPositions.Length > 0)
        {
            Handles.DrawAAPolyLine(45, movementPositions);
            Handles.Label(movementPositions.Last() + Vector3.right * uiScale, $"motion in 10s: {orbit.parameters.motionPerSecond * 10:0.0}°");
        }
        // Draw handles
        var meanLogitudeRot = Quaternion.Euler(0, 0, orbit.parameters.meanLongitude); //* Matrix4x4.Translate();
        bool DistAngleHandle()
        {

            Handles.color = Color.magenta;
            var distAngleHandlePos = Quaternion.Euler(0, 0, orbit.parameters.meanLongitude) * (Vector3.right * (orbit.parameters.meanDistance + 3f));
            Handles.DrawAAPolyLine(Vector3.zero, distAngleHandlePos);
            Handles.Label(distAngleHandlePos + Vector3.right * 2 * uiScale, $"mean dist: {orbit.parameters.meanDistance:0.0}\nmean long: {orbit.parameters.meanLongitude:0.0}°");
            EditorGUI.BeginChangeCheck();
            var newDistAngle = Handles.Slider2D(
                distAngleHandlePos,
                Vector3.forward,
                Vector3.right,
                Vector3.up,
                uiScale,
                Handles.CircleHandleCap, Vector2.zero);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(this.target, "Changed Slider Look Target");
                orbit.parameters.meanDistance = newDistAngle.magnitude - 3f;
                orbit.parameters.meanLongitude = Vector3.SignedAngle(Vector3.right, newDistAngle, Vector3.forward);
                orbit.RefreshValidateRecursive();
                return true;
            }
            return false;
        };
        DistAngleHandle();

        bool EccHandle()
        {
            Handles.color = Color.magenta;
            var distAngleHandlePos = Quaternion.Euler(0, 0, orbit.parameters.meanLongitude) * (Vector3.right * (orbit.parameters.meanDistance + 3f));
            Handles.color = Color.cyan;
            var eccHandlePos = meanLogitudeRot * (Vector3.down * orbit.parameters.meanDistance * (1.0f - orbit.parameters.eccentricity * 2f));
            Handles.Label(eccHandlePos + Vector3.right * uiScale, $"ecc: {orbit.parameters.eccentricity:0.00}");
            Handles.DrawAAPolyLine(Vector3.zero, distAngleHandlePos);
            EditorGUI.BeginChangeCheck();
            var newEcc = Handles.Slider(eccHandlePos, (meanLogitudeRot * Vector3.down).normalized, orbit.parameters.meanDistance * 0.25f * uiScale, Handles.ArrowHandleCap, 0);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(this.target, "Changed Slider Look Target");

                float newMag = Vector3.Dot(newEcc, eccHandlePos) <= 0 ? 0 : newEcc.magnitude;
                orbit.parameters.eccentricity =  Mathf.Clamp((1.0f - newMag / orbit.parameters.meanDistance) / 2f, 0, 0.3f);
                orbit.RefreshValidateRecursive();
                return true;
            }
            return false;
        };
        EccHandle();

        bool LopHandle()
        {
            Handles.color = Color.magenta;
            Handles.color = Color.yellow;
            var lopHandlePos = Quaternion.Euler(0, 0, orbit.parameters.longitudeOfPerihelion) * (Vector3.right * (orbit.parameters.meanDistance + 6f));
            Handles.DrawAAPolyLine(Vector3.zero, lopHandlePos);
            Handles.Label(lopHandlePos + Vector3.right * 2 * uiScale, $"lop: {orbit.parameters.longitudeOfPerihelion:0.0}");
            EditorGUI.BeginChangeCheck();
            var newLopAngle = Handles.Slider2D(
                lopHandlePos,
                Vector3.forward,
                Vector3.right,
                Vector3.up,
                0.6f * uiScale,
                Handles.CircleHandleCap, Vector2.zero);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(this.target, "Changed Slider Look Target");
                orbit.parameters.longitudeOfPerihelion = Vector3.SignedAngle(Vector3.right, newLopAngle, Vector3.forward);
                orbit.RefreshValidateRecursive();
                return true;
            }
            return false;
        };
        LopHandle();
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