using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class AsteroidRing : MonoBehaviour
{
    [Range(3, 30)]
    public float radius;
    [Range(0, 5)]
    public float width;
    public OrbitParameters.OrbitDirection direction;
    [Range(0, 5)]
    public float damageModifier = 1f;

    public ParticleSystem[] systems;

    public Transform[] shadows;
    [Range(0, 10)]
    public float shadowRateVariance = 3f;

    public RingDamageSource damageSource;

    // Degrees per second
    float orbitalAngularVelocity;
    readonly List<float> shadowAngularVelocities = new List<float>();

    Simulation simManager;

    void Awake()
    {
        this.simManager = FindObjectOfType<Simulation>();
    }

    void Start()
    {
        var parentGravitySource = this.GetComponentInParentOnly<GravitySource>();
        this.orbitalAngularVelocity = OrbitalUtils.OrbitalVelocityToAngularVelocity(this.radius, OrbitalUtils.Vp(this.radius, this.radius, parentGravitySource.parameters.mass, parentGravitySource.constants.GravitationalConstant)) * Mathf.Rad2Deg * (this.direction == OrbitParameters.OrbitDirection.Clockwise? -1f : 1f);

        foreach(var shadow in this.shadows)
        {
            shadow.localScale *= this.radius;
            this.shadowAngularVelocities.Add(Random.Range(-1, 1));
        }

        foreach(var system in this.systems)
        {
            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = this.radius;
            shape.radiusThickness = 0;
            shape.randomPositionAmount = this.width;
            system.Play();
        }

        if(this.damageSource != null)
        {
            this.damageSource.radius = this.radius;
            this.damageSource.range = this.width;
        }
    }

    void FixedUpdate()
    {
        float t = this.simManager == null ? Time.time : this.simManager.time;

        this.transform.localRotation = Quaternion.Euler(0, 0, t * this.orbitalAngularVelocity);

        for (int i = 0; i < this.shadows.Length; i++)
        {
            this.shadows[i].localRotation = Quaternion.Euler(0, 0, i * 71f + t * this.shadowAngularVelocities[i] * this.shadowRateVariance);
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        // Display the explosion radius when selected
        // Handles.color = this.gizmoColor;
        Handles.matrix = this.transform.localToWorldMatrix;
        Handles.DrawWireDisc(Vector3.zero, Vector3.forward, this.radius - this.width);
        Handles.DrawWireDisc(Vector3.zero, Vector3.forward, this.radius + this.width);
        GUIUtils.Label(Vector2.down * this.radius, "Asteroid ring");
    }
#endif
}
