/// <summary>
/// SURGE FRAMEWORK
/// Author: Bob Berkebile
/// Email: bobb@pixelplacement.com
/// 
/// Custom inspector for the DisplayObject class.
/// 
/// </summary>

using UnityEngine;
using System.Collections;
using UnityEditor;

namespace Pixelplacement
{
    [CustomEditor (typeof (DisplayObject), true)]
    public class DisplayObjectEditor : Editor 
    {
        //Private Variables:
        private DisplayObject _target;
        
        //Init:
        private void OnEnable()
        {
            _target = target as DisplayObject;
        }

        //Inspector GUI:
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector ();
            
            GUILayout.BeginHorizontal ();
            DrawSoloButton ();
            DrawShowButton ();
            DrawHideButton ();
            DrawHideAllButton ();
            GUILayout.EndHorizontal ();
        }
        
        //GUI Draw Methods:
        private void DrawShowButton ()
        {
            GUI.color = Color.yellow;
            if (GUILayout.Button ("Show"))
            {
                Undo.RegisterCompleteObjectUndo (_target, "Show DisplayObject");
                _target.SetActive (true);
            }
        }

        private void DrawSoloButton ()
        {
            GUI.color = Color.green;
            if (GUILayout.Button ("Solo"))
            {
                Undo.RegisterCompleteObjectUndo (_target, "Solo DisplayObject");
                _target.Solo ();
            }
        }

        private void DrawHideButton ()
        {
            GUI.color = new Color (1, 0.5f, 0, 1);
            if (GUILayout.Button ("Hide"))
            {
                Undo.RegisterCompleteObjectUndo (_target, "Hide DisplayObject");
                _target.SetActive (false);
            }
        }

        private void DrawHideAllButton ()
        {
            GUI.color = Color.red;
            if (GUILayout.Button ("Hide All"))
            {
                Undo.RegisterCompleteObjectUndo (_target, "Hide all DisplayObjects");
                _target.HideAll ();
            }
        }
    }
}