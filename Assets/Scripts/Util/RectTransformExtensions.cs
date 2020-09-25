using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class RectTransformExtensions
{
    public static Rect GetWorldRect(this RectTransform @this)
    {
        var rect = new Vector3[4];
        @this.GetWorldCorners(rect);
        return new Rect(rect[0], rect[2] - rect[0]);
    }
    public static Rect GetLocalRect(this RectTransform @this)
    {
        var rect = new Vector3[4];
        @this.GetLocalCorners(rect);
        return new Rect(rect[0], rect[2] - rect[0]);
    }
}
