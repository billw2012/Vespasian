using UnityEngine;
using UnityEngine.Assertions;

/// <summary>
/// Basic behaviour of a body in space, with a radius, axial rotation, and collision.
/// TODO: these features aren't really related, why are they in one component? Perhaps they always go together?
/// </summary>
public class BodyLogic : MonoBehaviour
{
    [Tooltip("Body Radius (use this instead of scaling)"), Range(0, 20)]
    public float radius = 0.5f;

    [Tooltip("Time to complete a full axial rotation (0 = no rotation)"), Range(-100, 100)]
    public float dayPeriod = 0;

    public Transform geometry;

    public GameLogic gameLogic;

    private float rotationOffset;
    private Simulation simulation;

    private void Awake()
    {
        this.simulation = FindObjectOfType<Simulation>();
        this.rotationOffset = Random.Range(0, 360f);
    }

    private void OnValidate()
    {
        this.UpdateScale();
    }

    private void Start()
    {
        this.UpdateScale();
    }

    private void FixedUpdate()
    {
        if (this.dayPeriod != 0)
        {
            float dt = this.simulation == null ? Time.deltaTime : this.simulation.dt;
            //var currEulers = this.geometry.localRotation.eulerAngles;
            this.geometry.localRotation *= Quaternion.Euler(0, 0, 360f * dt / this.dayPeriod);
        }
    }

    private void UpdateScale()
    {
        this.geometry.localScale = Vector3.one * this.radius;
        // Make sure to update auto mass
        var gravity = this.GetComponent<GravitySource>();
        if (gravity != null)
        {
            gravity.RefreshValidate();
        }
    }
}
