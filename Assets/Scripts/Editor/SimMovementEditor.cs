using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SimMovement))]
public class SimMovementEditor : Editor
{
    SimModel simModel;
    Task<SimPath> simTask;

    Vector3[] currPath;
    bool crashed;

    void OnSceneGUI()
    {
        if (Application.isPlaying)
            return;

        var simMovement = this.target as SimMovement;

        float uiScale = HandleUtility.GetHandleSize(simMovement.transform.position) * 0.2f;

        Handles.Label(simMovement.transform.position + (Vector3.right * 0.2f + Vector3.up) + simMovement.startVelocity * 0.5f, $"{simMovement.gameObject.name}", UnityEditor.EditorStyles.whiteLargeLabel);

        if (this.currPath != null && this.currPath.Length > 1)
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

            float scale = simMovement.constants.GameSpeedBase * 4f;
            var handlePos = simMovement.startVelocity * scale;
            Handles.DrawAAPolyLine(Vector3.zero, handlePos);
            Handles.Label(handlePos + Vector3.right * 2 * uiScale, $"velocity: {simMovement.startVelocity}\nshift = lock angle\nctrl = lock magnitude");
            EditorGUI.BeginChangeCheck();
            var newValue = Handles.Slider2D(
                handlePos,
                Vector3.forward,
                Vector3.right,
                Vector3.up,
                uiScale * 0.5f,
                Handles.CircleHandleCap,
                Vector2.zero) / scale;
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(this.target, "Changed velocity");
                if (Event.current.control)
                {
                    simMovement.startVelocity = newValue.normalized * simMovement.startVelocity.magnitude;
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


        if(this.simTask?.Status == TaskStatus.RanToCompletion)
        {
            var path = this.simTask.Result.path.AsEnumerable();
            if(path.Count() % 2 == 1)
            {
                path = path.Take(path.Count() - 1);
            }
            this.currPath = path.ToArray();
            this.crashed = false; // this.simTask.Result.crashed;
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
            this.simTask = this.simModel.CalculateSimPath(
                simMovement.transform.position,
                simMovement.startVelocity,
                0,
                Time.fixedDeltaTime * simMovement.constants.GameSpeedBase,
                5000,
                0,
                simMovement.constants.GravitationalConstant,
                simMovement.constants.GravitationalRescaling
            );
        }
    }
}
