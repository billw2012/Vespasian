using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

public static class GUIUtils
{
    public static void Label(Vector2 pos, string text)
    {
        var labelStyle = GUI.skin.box;
        labelStyle.normal.textColor = Color.white;
        Handles.Label(pos, text, labelStyle);
    }
}
