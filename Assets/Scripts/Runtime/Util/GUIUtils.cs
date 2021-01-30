using UnityEditor;
using UnityEngine;

#if UNITY_EDITOR
public static class GUIUtils
{
    public static void Label(Vector2 pos, string text)
    {
        var labelStyle = GUI.skin.box;
        labelStyle.normal.textColor = Color.white;
        Handles.Label(pos, text, labelStyle);
    }
}
#endif