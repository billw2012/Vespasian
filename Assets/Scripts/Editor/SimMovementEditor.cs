using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SimMovement))]
public class SimMovementEditor : Editor
{
    SimModel simModel;
    Task<SimModel.SimState> simTask;

    Vector3[] currPath;
    bool crashed;

    void OnSceneGUI()
    {
        if (Application.isPlaying)
            return;

        var simMovement = this.target as SimMovement;

        float uiScale = HandleUtility.GetHandleSize(simMovement.transform.position) * 0.2f;

        Handles.Label(simMovement.transform.position + (Vector3.right * 0.2f + Vector3.up) + simMovement.velocity * 0.5f, $"{simMovement.gameObject.name}", UnityEditor.EditorStyles.whiteLargeLabel);

        if (this.currPath != null)
        {
            Handles.DrawLines(this.currPath);
            if(this.crashed)
            {
                Handles.color = Color.red;
                var crashPt = this.currPath.Last();
                Handles.DrawLine(crashPt + Vector3.left + Vector3.down, crashPt + Vector3.right + Vector3.up);
                Handles.DrawLine(crashPt + Vector3.right + Vector3.down, crashPt + Vector3.left + Vector3.up);
            }
        }

        bool VelocityHandle()
        {
            Handles.color = Color.magenta;
            Handles.matrix = Matrix4x4.Translate(simMovement.transform.position - simMovement.transform.right * uiScale * 1);

            var handlePos = simMovement.velocity * 4f;
            Handles.DrawAAPolyLine(Vector3.zero, handlePos);
            Handles.Label(handlePos + Vector3.right * 2 * uiScale, $"velocity: {simMovement.velocity}");
            EditorGUI.BeginChangeCheck();
            var newValue = Handles.Slider2D(
                handlePos,
                Vector3.forward,
                Vector3.right,
                Vector3.up,
                uiScale * 0.5f,
                Handles.CircleHandleCap,
                Vector2.zero) / 4f;
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(this.target, "Changed velocity");
                if (Event.current.control)
                {
                    simMovement.velocity = newValue.normalized * simMovement.velocity.magnitude;
                }
                else if(Event.current.shift)
                {
                    simMovement.velocity = simMovement.transform.up * newValue.magnitude;
                }
                else
                {
                    simMovement.velocity = newValue;
                }
                if (simMovement.alignToVelocity)
                {
                    simMovement.transform.localRotation = Quaternion.FromToRotation(Vector3.up, newValue);
                }
                return true;
            }
            return false;
        };
        VelocityHandle();


        if(this.simTask?.Status == TaskStatus.RanToCompletion)
        {
            var path = this.simTask.Result.path.AsEnumerable();
            if(path.Count() % 2 == 1)
            {
                path = path.Take(path.Count() - 1);
            }
            this.currPath = path.ToArray();
            this.crashed = this.simTask.Result.crashed;
            this.simTask = null;
        }
        else if(this.simTask?.Status == TaskStatus.Faulted)
        {
            this.currPath = null;
        }

        if (this.simTask == null)
        {
            if(this.simModel == null)
            {
                this.simModel = new SimModel();
            }
            this.simTask = this.simModel.CalculateSimState(
                simMovement.transform.position,
                simMovement.velocity,
                0,
                Time.fixedDeltaTime,
                1000,
                0,
                simMovement.constants.GravitationalConstant);
        }
    }
}
