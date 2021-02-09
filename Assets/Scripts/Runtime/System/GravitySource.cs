

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public struct GravityParameters
{
    [Tooltip("Density used if auto-calculating the mass from the radius")]
    public float density;
    [Tooltip("Mass, if less than zero then it will be automatically calculated based on density and radius"), Range(0, 500)]
    public float mass;
}

/// <summary>
/// Describes parameters related to gravity for a body.
/// </summary>
public class GravitySource : MonoBehaviour {
    public GravityParameters parameters = new GravityParameters {
        density = 1,
        mass = 0
    };

    public GameConstants constants;

    [Tooltip("Transform to use as the gravity source origin")]
    public Transform customTarget;

    [NonSerialized]
    public Transform target;

    public float radius => this.target.localScale.x;

    public Vector3 position => this.target.position;

    [Tooltip("Mass will be automatically calculated from radius and density")]
    public bool autoMass = true;

    [NonSerialized]
    public Color color;

    private void OnValidate()
    {
        this.RefreshValidate();
    }

    private void Awake()
    {
        this.RefreshValidate();
        this.color = UnityEngine.Random.ColorHSV(0, 1, 0.75f, 0.75f, 1, 1);
    }

    public static List<GravitySource> All() => ComponentCache.FindObjectsOfType<GravitySource>().OrderBy(o => o.GetInstanceID()).ToList();

    public void RefreshValidate()
    {
        this.target = this.customTarget == null ? this.transform : this.customTarget;

        if (this.autoMass)
        {
            // We are using area instead of volume for mass, or we can't vary size much without 
            // having extreme results
            this.parameters.mass = Mathf.PI * Mathf.Pow(this.radius, 2) * this.parameters.density;
        }
    }
}