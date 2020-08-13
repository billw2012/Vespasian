

using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct GravityParameters
{
    [Tooltip("Density used if auto-calculating the mass from the radius"), Range(0, 1)]
    public float density;
    [Tooltip("Mass, if less than zero then it will be automatically calculated based on density and radius")]
    public float mass;

    public static Vector3 CalculateForce(Vector3 from, Vector3 to, float toMass)
    {
        var vec = to - from;
        return GameConstants.Instance.GlobalCoefficient * GameConstants.Instance.AccelerationCoefficient * vec.normalized * toMass / Mathf.Pow(vec.magnitude, 2);
    }
}

public class GravitySource : MonoBehaviour {
    public GravityParameters parameters = new GravityParameters {
        density = 1,
        mass = -1
    };

    public static HashSet<GravitySource> All = new HashSet<GravitySource>();

    void Start()
    {
        All.Add(this);
        if(this.parameters.mass < 0)
        {
            // We are using area instead of volume for mass, or we can't vary size much without 
            // having extreme results
            this.parameters.mass = Mathf.PI * Mathf.Pow(this.transform.localScale.x, 2) * this.parameters.density;
        }
    }

    void OnDestroy()
    {
        All.Remove(this);
    }

}