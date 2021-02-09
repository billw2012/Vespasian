using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Object = UnityEngine.Object;

class ComponentCache
{
    static Dictionary<Type, List<UnityEngine.Object>> cache = new Dictionary<Type, List<UnityEngine.Object>>();

    public static T[] FindObjectsOfType<T>() where T:UnityEngine.Object
    {
        Dictionary<Type, List<UnityEngine.Object>> _cache = ComponentCache.cache;
        Type t = typeof(T);
        if (cache.ContainsKey(t))
        {
            List<UnityEngine.Object> compList;
            _cache.TryGetValue(t, out compList);

            foreach (UnityEngine.Object obj in compList)
            {
                if (obj == null)
                {
                    Debug.Log("Breakpoint!");
                }
            }

            // Ensure we don't return any null-values
            // todo: it might have performance impact
            compList.RemoveAll(i => i == null);

            UnityEngine.Object[] compArray = compList.ToArray();
            T[] compArrayCast = Array.ConvertAll<UnityEngine.Object, T>(compArray, item => (T)item);
            return compArrayCast;
        }
        else
        {
            UnityEngine.Object[] compArray = UnityEngine.Object.FindObjectsOfType(t);
            List<UnityEngine.Object> compList = compArray.ToList<UnityEngine.Object>();
            _cache.Add(t, compList);
            return (T[]) compArray;
        }
    }

    public static GameObject Instantiate(GameObject original)
    {
        GameObject obj = Object.Instantiate(original);
        ResetCache(obj);
        return obj;
    }
    public static GameObject Instantiate(GameObject original, Transform parent)
    {
        GameObject obj = Object.Instantiate(original, parent);
        ResetCache(obj);
        return obj;
    }

    public static GameObject Instantiate(GameObject original, Transform parent, bool instantiateInWorldSpace)
    {
        GameObject obj = Object.Instantiate(original, parent, instantiateInWorldSpace);
        ResetCache(obj);
        return obj;
    }

    public static GameObject Instantiate(GameObject original, Vector3 position, Quaternion rotation)
    {
        GameObject obj = Object.Instantiate(original, position, rotation);
        ResetCache(obj);
        return obj;
    }

    public static GameObject Instantiate(GameObject original, Vector3 position, Quaternion rotation, Transform parent)
    {
        GameObject obj = Object.Instantiate(original, position, rotation, parent);
        ResetCache(obj);
        return obj;
    }

    static void ResetCache(GameObject obj)
    {
        Type tEnd = typeof(UnityEngine.Component);

        // Flush all caches of types of components in this object
        // Also flush caches of all base types until we reach MonoBehaviour
        // We don't add all components to lists because we don't want to cache all of them and we don't know what types we want to cache
        foreach (Component component in obj.GetComponentsInChildren<Component>())
        {
            Type t = component.GetType();
            while (true)
            {
                if (cache.ContainsKey(t))
                {
                    cache.Remove(t);
                }

                if (t == tEnd)
                    break;

                t = t.BaseType;

                if (t == null)
                    break;
            }
        }
    }
}
