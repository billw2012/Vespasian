using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class ComponentCache
{
    private static readonly Dictionary<Type, List<UnityEngine.Object>> cache = new Dictionary<Type, List<UnityEngine.Object>>();

    static ComponentCache()
    {
        // Reset entire cache on scene change of course
        SceneManager.sceneUnloaded += _ => cache.Clear();
    }
    
    public static T[] FindObjectsOfType<T>() where T:UnityEngine.Object
    {
        if (cache.TryGetValue(typeof(T), out var compList))
        {
            // Ensure we don't return any null-values
            // todo: it might have performance impact
            compList.RemoveAll(i => i == null);
            return Array.ConvertAll(compList.ToArray(), item => (T)item);
        }
        else
        {
            var compArray = UnityEngine.Object.FindObjectsOfType(typeof(T));
            cache.Add(typeof(T), compArray.ToList());
            return (T[]) compArray;
        }
    }

    public static T FindObjectOfType<T>() where T : UnityEngine.Object
    {
        if (cache.TryGetValue(typeof(T), out var compList))
        {
            // Ensure we don't return any null-values
            // todo: it might have performance impact
            compList.RemoveAll(i => i == null);

            // Return first element if there is any
            return compList.Count > 0 ? (T)compList[0] : null;
        }
        else
        {
            compList = UnityEngine.Object.FindObjectsOfType(typeof(T)).ToList();
            cache.Add(typeof(T), compList);

            // Return first element if there is any
            return compList.Count > 0 ? (T)compList[0] : null;
        }
    }

    public static GameObject Instantiate(GameObject original)
    {
        var obj = Object.Instantiate(original);
        ResetCache(obj);
        return obj;
    }

    public static GameObject Instantiate(GameObject original, Transform parent)
    {
        var obj = Object.Instantiate(original, parent);
        ResetCache(obj);
        return obj;
    }

    public static GameObject Instantiate(GameObject original, Transform parent, bool instantiateInWorldSpace)
    {
        var obj = Object.Instantiate(original, parent, instantiateInWorldSpace);
        ResetCache(obj);
        return obj;
    }

    public static GameObject Instantiate(GameObject original, Vector3 position, Quaternion rotation)
    {
        var obj = Object.Instantiate(original, position, rotation);
        ResetCache(obj);
        return obj;
    }

    public static GameObject Instantiate(GameObject original, Vector3 position, Quaternion rotation, Transform parent)
    {
        var obj = Object.Instantiate(original, position, rotation, parent);
        ResetCache(obj);
        return obj;
    }

    private static void ResetCache(GameObject obj)
    {
        IEnumerable<System.Type> GetAllTypes(System.Type t, System.Type last)
        {
            while (t != last && t != null)
            {
                yield return t;
                t = t.BaseType;
            }
            if (t == last)
            {
                yield return t;
            }
        }
        
        // Flush all caches of types of components in this object
        // Also flush caches of all base types until we reach MonoBehaviour
        // We don't add all components to lists because we don't want to cache all of them and we don't know what types we want to cache
        foreach (var t in obj.GetComponentsInChildren<Component>()
            .SelectMany(c => GetAllTypes(c.GetType(), typeof(UnityEngine.Component)))
        )
        {
            cache.Remove(t);
        }
    }
}
