

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

[Serializable]
public struct GravityParameters
{
    [Tooltip("Density used if auto-calculating the mass from the radius"), Range(0, 1)]
    public float density;
    [Tooltip("Mass, if less than zero then it will be automatically calculated based on density and radius"), Range(0, 500)]
    public float mass;
}

public class GravitySource : MonoBehaviour {
    public GravityParameters parameters = new GravityParameters {
        density = 1,
        mass = 0
    };

    public GameConstants constants;

    [Tooltip("Transform to use as the gravity source origin")]
    public Transform customTarget;

    [HideInInspector]
    public Transform target;

    [HideInInspector]
    public float radius => this.target.localScale.x * 0.5f; // The sphere model generally has radius 0.5, so scale 1 means size 0.5..

    [HideInInspector]
    public Vector3 position => this.target.position;

    [Tooltip("Mass will be automatically calculated from radius and density")]
    public bool autoMass = true;

    void OnValidate()
    {
        this.RefreshValidate();
    }

    void Start()
    {
        this.RefreshValidate();
    }

    public static List<GravitySource> All()
    {
        return GameObject.FindObjectsOfType<GravitySource>().OrderBy(o => o.GetInstanceID()).ToList();
    }

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