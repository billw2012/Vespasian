using System.Linq;
using UnityEngine;

/// <summary>
/// Small helper class to convert viewport, screen or world positions to canvas space.
/// Only works with screen space canvases.
/// Usage:
/// objectOnCanvasRectTransform.anchoredPosition = specificCanvas.WorldToCanvasPoint(worldspaceTransform.position);
/// </summary>
/// 
public static class CanvasPositioningExtensions
{
    public static Vector2 WorldToCanvasPosition(this Canvas canvas, Vector3 worldPosition, Camera camera = null)
    {
        if (camera == null)
        {
            camera = GUILayerManager.MainCamera;
        }
        var viewportPosition = camera.WorldToViewportPoint(worldPosition);
        return canvas.ViewportToCanvasPosition(viewportPosition);
    }

    public static Vector3 CanvasToWorldPosition(this Canvas canvas, Vector2 canvasPosition, float z = 0, Camera camera = null)
    {
        if (camera == null)
        {
            camera = GUILayerManager.MainCamera;
        }

        var viewportPosition = canvas.CanvasToViewportPosition(canvasPosition);
        return  camera.ViewportToWorldPoint((Vector3)viewportPosition + Vector3.forward * z);
    }

    public static Vector2 ScreenToCanvasPosition(this Canvas canvas, Vector2 screenPosition)
    {
        var viewportPosition = new Vector2(screenPosition.x / Screen.width,
                                           screenPosition.y / Screen.height);
        return canvas.ViewportToCanvasPosition(viewportPosition);
    }

    
    
    
    public static Rect ScreenToCanvasRect(this Canvas canvas, Rect screenRect)
    {
        var viewportRectMin = new Vector2(screenRect.xMin / Screen.width,
                                           screenRect.yMin / Screen.height);
        var viewportRectMax = new Vector2(screenRect.xMax / Screen.width,
                                           screenRect.yMax / Screen.height);
        var canvasRectMin = canvas.ViewportToCanvasPosition(viewportRectMin);
        return new Rect(canvasRectMin, canvas.ViewportToCanvasPosition(viewportRectMax)  - canvasRectMin);
    }

    public static Vector2 ViewportToCanvasPosition(this Canvas canvas, Vector2 viewportPosition)
    {
        var canvasRect = canvas.GetComponent<RectTransform>();
        var scale = canvasRect.sizeDelta;
        return Vector2.Scale(viewportPosition, scale);
    }

    public static Vector2 CanvasToViewportPosition(this Canvas canvas, Vector2 canvasPosition)
    {
        var canvasRect = canvas.GetComponent<RectTransform>();
        var scale = canvasRect.sizeDelta;
        return Vector2.Scale(canvasPosition, Vector2.one / scale);
    }

    public static Vector3[] GetWorldSpaceRect(this Canvas canvas, RectTransform ui, float z = 0, Camera camera = null)
    {
        var canvasRect = new Vector3[4];
        ui.GetWorldCorners(canvasRect);
        return canvasRect.Select(p => canvas.CanvasToWorldPosition(p, z, camera)).ToArray();
    }
    //public static Vector2 CanvasToAnchoredPosition(this RectTransform transform, Vector2 canvasPosition)
    //{
    //    ((RectTransform)transform.parent).Get
    //}
}