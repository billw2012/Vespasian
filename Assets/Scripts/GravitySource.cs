

using System.Collections.Generic;
using UnityEngine;

public class GravitySource : MonoBehaviour {
    public float density = 1.0f;
    public float mass = -1.0f;

    public static HashSet<GravitySource> All = new HashSet<GravitySource>();

    void Start()
    {
        All.Add(this);
        if(this.mass < 0)
        {
            // We are using area instead of volume for mass, or we can't vary size much without 
            // having extreme results
            this.mass = Mathf.PI * Mathf.Pow(this.transform.localScale.x, 2) * this.density;
        }
    }

    void OnDestroy()
    {
        All.Remove(this);
    }

    public static Vector3 CalculateForce(Vector3 from, Vector3 to, float toMass)
    {
        var vec = to - from;
        return GameConstants.Instance.GlobalCoefficient * GameConstants.Instance.AccelerationCoefficient * vec.normalized * toMass / Mathf.Pow(vec.magnitude, 2);
    }
}