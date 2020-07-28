

using System.Collections.Generic;
using UnityEngine;

public class GravitySource : MonoBehaviour {
    public float Mass = 1.0f;

    public static HashSet<GravitySource> All = new HashSet<GravitySource>();

    void Start()
    {
        All.Add(this);
    }

    void OnDestroy()
    {
        All.Remove(this);
    }
}