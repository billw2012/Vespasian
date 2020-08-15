using System;
using System.Collections.Generic;
using UnityEngine;

// From https://stackoverflow.com/a/10660969/6402065
public static class UnityExtensions
{
    public static Vector2 Abs(this Vector2 vector)
    {
        for (int i = 0; i < 2; ++i) vector[i] = Mathf.Abs(vector[i]);
        return vector;
    }

    public static Vector2 DividedBy(this Vector2 vector, Vector2 divisor)
    {
        for (int i = 0; i < 2; ++i) vector[i] /= divisor[i];
        return vector;
    }

    public static Vector2 Max(this Rect rect)
    {
        return new Vector2(rect.xMax, rect.yMax);
    }

    public static Vector2 IntersectionWithRayFromCenter(this Rect rect, Vector2 pointOnRay)
    {
        Vector2 pointOnRay_local = pointOnRay - rect.center;
        Vector2 edgeToRayRatios = (rect.Max() - rect.center).DividedBy(pointOnRay_local.Abs());
        return (edgeToRayRatios.x < edgeToRayRatios.y) ?
            new Vector2(pointOnRay_local.x > 0 ? rect.xMax : rect.xMin,
                pointOnRay_local.y * edgeToRayRatios.x + rect.center.y) :
            new Vector2(pointOnRay_local.x * edgeToRayRatios.y + rect.center.x,
                pointOnRay_local.y > 0 ? rect.yMax : rect.yMin);
    }

    public static Vector2 ClampToRectOnRay(this Rect rect, Vector2 point)
    {
        return rect.Contains(point) ? point : rect.IntersectionWithRayFromCenter(point);
    }

    public static T GetComponentInParentOnly<T>(this GameObject child) where T : class
    {
        #pragma warning disable UNT0014 // Invalid type for call to GetComponent
        var parent = child.transform.parent;
        while (parent != null && !parent.HasComponent<T>())
        {
            parent = parent.transform.parent;
        }
        return parent != null ? parent.GetComponent<T>() : null;
        #pragma warning restore UNT0014 // Invalid type for call to GetComponent
    }

    public static IEnumerable<GameObject> GetAllParents(this GameObject child, GameObject until = null)
    {
        var parent = child.transform.parent;
        while (parent != null && parent.gameObject != until)
        {
            yield return parent.gameObject;
            parent = parent.transform.parent;
        }
    }

    public static IEnumerable<GameObject> GetAllParents(this Component child, GameObject until = null)
    {
        var parent = child.transform.parent;
        while (parent != null && parent.gameObject != until)
        {
            yield return parent.gameObject;
            parent = parent.transform.parent;
        }
    }

    public static bool IsIdentity(this Transform transform)
    {
        return transform.localPosition == Vector3.zero
            && transform.localScale == Vector3.one
            && transform.localRotation == Quaternion.identity;
    }

    public static bool HasComponent<T>(this GameObject g) where T : class
    {
        #pragma warning disable UNT0014 // Invalid type for call to GetComponent
        return g.GetComponent<T>() != null;
#pragma warning restore UNT0014 // Invalid type for call to GetComponent
    }

    public static bool HasComponent<T>(this Component c) where T : class
    {
#pragma warning disable UNT0014 // Invalid type for call to GetComponent
        return c.GetComponent<T>() != null;
#pragma warning restore UNT0014 // Invalid type for call to GetComponent
    }
}
