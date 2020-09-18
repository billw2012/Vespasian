﻿using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(SimMovement))]
public class SimMovementEditor : Editor
{
    SimModel simModel;

    bool editing = false;
    float scale;

    void OnSceneGUI()
    {
        if (Application.isPlaying)
            return;

        var simMovement = this.target as SimMovement;

        float uiScale = HandleUtility.GetHandleSize(simMovement.transform.position) * 0.2f;

        Handles.Label(simMovement.transform.position + (Vector3.right * 0.2f + Vector3.up) + simMovement.startVelocity * 0.5f, $"{simMovement.gameObject.name}", UnityEditor.EditorStyles.whiteLargeLabel);

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
            Handles.Label(handlePos + Vector3.right * 2 * uiScale, $"velocity: {simMovement.startVelocity}\nshift = lock angle\nctrl = lock magnitude");
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


        //if(this.simTask?.Status == TaskStatus.RanToCompletion)
        //{
        //    var path = this.simTask.Result.path.AsEnumerable();
        //    if(path.Count() % 2 == 1)
        //    {
        //        path = path.Take(path.Count() - 1);
        //    }
        //    this.currPath = path.ToArray();
        //    this.crashed = this.simTask.Result.crashed; // this.simTask.Result.crashed;
        //    this.simTask = null;
        //}
        //else if(this.simTask?.Status == TaskStatus.Faulted)
        //{
        //    this.currPath = null;
        //}

        //if (this.simTask == null)
        //{
        //    if(this.simModel == null)
        //    {
        //        this.simModel = new SimModel();
        //    }
        //    this.simTask = this.simModel.CalculateSimPath(
        //        simMovement.transform.position,
        //        simMovement.startVelocity,
        //        0,
        //        Time.fixedDeltaTime,
        //        5000,
        //        1,
        //        simMovement.constants.GravitationalConstant,
        //        simMovement.constants.GravitationalRescaling
        //    );
        //}
    }

    static double lastUpdate = 0;

    [DrawGizmo(GizmoType.NotInSelectionHierarchy | GizmoType.InSelectionHierarchy)]
    static async void RenderCustomGizmo(SimMovement simMovement, GizmoType gizmoType)
    {
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
