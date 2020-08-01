

using System.Collections.Generic;
using UnityEngine;

public class GravitySource : MonoBehaviour {
    public float Density = 1.0f;
    public float Mass = -1.0f;

    public static HashSet<GravitySource> All = new HashSet<GravitySource>();

    void Start()
    {
        All.Add(this);
        if(this.Mass < 0)
        {
            // We are using area instead of volume for mass, or we can't vary size much without 
            // having extreme results
            this.Mass = Mathf.PI * Mathf.Pow(this.transform.localScale.x, 2) * this.Density;
        }
    }

    void OnDestroy()
    {
        All.Remove(this);
    }
}