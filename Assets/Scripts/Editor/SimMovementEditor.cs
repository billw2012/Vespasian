using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SimMovement))]
public class SimMovementEditor : Editor
{
    private bool editing = false;
    private float scale;

    private void OnSceneGUI()
    {
        if (Application.isPlaying)
            return;

        var simMovement = this.target as SimMovement;

        float uiScale = HandleUtility.GetHandleSize(simMovement.transform.position) * 0.2f;

        Handles.Label((Vector2)simMovement.transform.position + (Vector2.right * 0.2f + Vector2.up) + simMovement.startVelocity * 0.5f, $"{simMovement.gameObject.name}", UnityEditor.EditorStyles.whiteLargeLabel);

        if (simMovement.editorCurrPath?.Length > 1)
        {
            Handles.DrawLines(simMovement.editorCurrPath);
            if(simMovement.editorCrashed)
            {
                Handles.color = Color.red;
                var crashPt = simMovement.editorCurrPath.Last();
                Handles.DrawLine(crashPt + Vector3.left + Vector3.down, crashPt + Vector3.right + Vector3.up);
                Handles.DrawLine(crashPt + Vector3.right + Vector3.down, crashPt + Vector3.left + Vector3.up);
            }
        }

        bool VelocityHandle()
        {
            int id = GUIUtility.GetControlID(FocusType.Passive);

            if(!this.editing)
            {
                this.editing = Event.current.GetTypeForControl(id) == EventType.MouseDown;
                this.scale = 2 * HandleUtility.GetHandleSize(Vector3.zero) / Mathf.Max(0.0001f, simMovement.startVelocity.magnitude);
            }
            else
            {
                this.editing = Event.current.GetTypeForControl(id) != EventType.MouseUp;
            }

            Handles.color = Color.magenta;
            Handles.matrix = Matrix4x4.Translate(simMovement.transform.position - simMovement.transform.right * uiScale * 1);

            //float scale = simMovement.constants.GameSpeedBase * 4f;
            var handlePos = simMovement.startVelocity * this.scale;
            Handles.DrawAAPolyLine(Vector3.zero, handlePos);
            Handles.Label(handlePos + Vector2.right * 2 * uiScale, $"velocity: {simMovement.startVelocity}\nshift = lock angle\nctrl = lock magnitude");
            EditorGUI.BeginChangeCheck();
            var newValue = Handles.Slider2D(
                id,
                handlePos,
                Vector3.forward,
                Vector3.right,
                Vector3.up,
                uiScale * 0.5f,
                Handles.CircleHandleCap,
                Vector2.zero) / this.scale;


            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(this.target, "Changed velocity");
                if (Event.current.control)
                {
                    simMovement.startVelocity = newValue.normalized * Mathf.Max(1, simMovement.startVelocity.magnitude);
                    if (simMovement.alignToVelocity)
                    {
                        simMovement.transform.localRotation = Quaternion.FromToRotation(Vector3.up, newValue);
                    }
                }
                else if(Event.current.shift)
                {
                    simMovement.startVelocity = simMovement.transform.up * newValue.magnitude;
                }
                else
                {
                    simMovement.startVelocity = newValue;
                    if (simMovement.alignToVelocity)
                    {
                        simMovement.transform.localRotation = Quaternion.FromToRotation(Vector3.up, newValue);
                    }
                }
                return true;
            }
            return false;
        };
        VelocityHandle();
    }

    private static double lastUpdate = 0;

    [DrawGizmo(GizmoType.NotInSelectionHierarchy | GizmoType.InSelectionHierarchy)]
    private static async void RenderCustomGizmo(SimMovement simMovement, GizmoType gizmoType)
    {
        // Draw collision radius
        Handles.color = new Color(0.16f, 0.66f, 0.66f);
        Handles.DrawWireDisc(simMovement.transform.position, Vector3.forward, simMovement.collisionRadius);
        
        // Draw existing paths
        Handles.color = new Color(0.33f, 0.66f, 0.66f);

        if(simMovement.editorCurrPath?.Length > 1)
        {
            Handles.DrawLines(simMovement.editorCurrPath);
            if (simMovement.editorCrashed)
            {
                Handles.color = Color.red;
                var crashPt = simMovement.editorCurrPath.Last();
                Handles.DrawLine(crashPt + Vector3.left + Vector3.down, crashPt + Vector3.right + Vector3.up);
                Handles.DrawLine(crashPt + Vector3.right + Vector3.down, crashPt + Vector3.left + Vector3.up);
            }
        }

        var sun = FindObjectsOfType<StarLogic>()
            .FirstOrDefault(l => l.isPrimary)
            ?.GetComponent<GravitySource>();
        if (sun == null)
        {
            return;
        }

        var orbit = AnalyticOrbit.FromCartesianStateVector(
            simMovement.transform.position, 
            Application.isPlaying? simMovement.velocity : (Vector3)simMovement.startVelocity, 
            sun.parameters.mass,
            sun.constants.GravitationalConstant);
        Handles.color = Color.yellow;
        var orbitPath = orbit.GetPath();
        Handles.DrawPolyLine(orbitPath);
        Handles.DrawWireCube(orbitPath.First(), Vector3.one);
        Handles.color = Color.red;
        Handles.DrawWireCube(orbitPath.Skip(10).First(), Vector3.one);
        Handles.Label(orbitPath.First() + Vector3.right * 4, 
            $"meanLongitude: {orbit.meanLongitude:0.0}\n" +
            $"t pe: {orbit.timeOfPeriapsis:0.0}\n" +
            $"t ae: {orbit.timeOfApoapsis:0.0}\n" +
            $"eccentricity: {orbit.eccentricity}\n" + 
            $"argumentOfPeriapsis: {orbit.argumentOfPeriapsis}\n" +
            $"motionPerSecond: {orbit.motionPerSecond}\n" +
            $"semiMajorAxis: {orbit.semiMajorAxis}"
            );
        
        if(!EditorApplication.isPlaying && EditorApplication.timeSinceStartup - lastUpdate > 0.05)
        {
            lastUpdate = EditorApplication.timeSinceStartup;
            var simModel = new SimModel();
            simModel.DelayedInit();
            foreach (var s in FindObjectsOfType<SimMovement>())
            {
                await s.EditorUpdatePathAsync(simModel);
            }
        }
    }
}
