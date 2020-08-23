using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Orbit))]
public class OrbitEditor : Editor
{
    private static readonly int ControlIDBase = typeof(OrbitEditor).ToString().GetHashCode();

    void OnSceneGUI()
    {
        var orbit = this.target as Orbit;

        // Draw orbit
        Handles.matrix = orbit.transform.localToWorldMatrix;
        var positions = orbit.GetPositions();
        Handles.DrawAAPolyLine(orbit.GetPositions());
        Handles.DrawAAPolyLine(Vector3.zero, positions[0]);

        // Draw movement per 10 seconds
        Handles.matrix = orbit.transform.localToWorldMatrix;
        Handles.color = new Color(0, 1, 0, 0.5f);
        var movementPositions = orbit.GetPositions(Mathf.Abs(orbit.parameters.motionPerSecond) * 10);
        if (movementPositions.Length > 0)
        {
            Handles.DrawAAPolyLine(45, movementPositions);
            Handles.Label(movementPositions.Last() + Vector3.right, $"motion in 10s: {orbit.parameters.motionPerSecond * 10:0.0}°");
        }
        // Draw handles
        var meanLogitudeRot = Quaternion.Euler(0, 0, orbit.parameters.meanLongitude); //* Matrix4x4.Translate();
        bool DistAngleHandle()
        {

            Handles.color = Color.magenta;
            var distAngleHandlePos = Quaternion.Euler(0, 0, orbit.parameters.meanLongitude) * (Vector3.right * (orbit.parameters.meanDistance + 3f));
            Handles.DrawAAPolyLine(Vector3.zero, distAngleHandlePos);
            Handles.Label(distAngleHandlePos + Vector3.right * 2, $"mean dist: {orbit.parameters.meanDistance:0.0}\nmean long: {orbit.parameters.meanLongitude:0.0}°");
            EditorGUI.BeginChangeCheck();
            var newDistAngle = Handles.Slider2D(
                distAngleHandlePos,
                Vector3.forward,
                Vector3.right,
                Vector3.up,
                1f,
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
            Handles.Label(eccHandlePos + Vector3.right * 2, $"ecc: {orbit.parameters.eccentricity:0.00}");
            Handles.DrawAAPolyLine(Vector3.zero, distAngleHandlePos);
            EditorGUI.BeginChangeCheck();
            var newEcc = Handles.Slider(eccHandlePos, (meanLogitudeRot * Vector3.down).normalized, orbit.parameters.meanDistance * 0.5f, Handles.ArrowHandleCap, 0);
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
            Handles.Label(lopHandlePos + Vector3.right * 2, $"lop: {orbit.parameters.longitudeOfPerihelion:0.0}");
            EditorGUI.BeginChangeCheck();
            var newLopAngle = Handles.Slider2D(
                lopHandlePos,
                Vector3.forward,
                Vector3.right,
                Vector3.up,
                0.6f,
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
}