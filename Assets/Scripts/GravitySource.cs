

using System;
using System.Collections.Generic;
using System.Linq;
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

    void Start()
    {
        bool Validate()
        {
            var parentOrbit = this.gameObject.GetComponentInParent<Orbit>();
            // If it hasn't got a parent orbit then any transform is valid
            if (parentOrbit == null)
                return true;

            // Only game objects being controlled by Orbit component are allowed to have non-identity transforms
            var orbitControlled = GameObject.FindObjectsOfType<Orbit>().Select(o => o.position.gameObject);
            // If it has a parent then all the non orbit parents must have null local transforms
            var invalidParents = this.gameObject.GetAllParents(until: parentOrbit.gameObject)
                .Where(p => !orbitControlled.Contains(p) && !p.transform.IsIdentity());
            if (invalidParents.Any())
            {
                foreach (var p in invalidParents)
                {
                    Debug.LogError($"{this}: parent {p.name} is invalid, it has none zero transform", p);
                }
                return false;
            }
            return true;
        }
        Debug.Assert(Validate());

        if(this.parameters.mass < 0)
        {
            // We are using area instead of volume for mass, or we can't vary size much without 
            // having extreme results
            this.parameters.mass = Mathf.PI * Mathf.Pow(this.transform.localScale.x, 2) * this.parameters.density;
        }
    }

    public static GravitySource[] All()
    {
        return GameObject.FindObjectsOfType<GravitySource>().OrderBy(o => o.GetInstanceID()).ToArray();
    }
}