using UnityEngine;
using UnityEngine.Assertions;

public class BodyLogic : MonoBehaviour
{
    [Tooltip("Planet Radius (use this instead of scaling)"), Range(0, 20)]
    public float radius = 0.5f;

    [Tooltip("Time to complete a full axial rotation (0 = no rotation)"), Range(-100, 100)]
    public float dayPeriod = 0;

    public Transform geometry;

    public GameLogic gameLogic;

    float rotationOffset;
    Simulation simManager;

    void Awake()
    {
        this.simManager = FindObjectOfType<Simulation>();
        this.rotationOffset = Random.Range(0, 360f);
    }

    void OnValidate()
    {
        this.UpdateScale();
    }

    void Start()
    {
        this.UpdateScale();
    }

    void FixedUpdate()
    {
        if (this.dayPeriod != 0)
        {
            float dt = this.simManager == null ? Time.deltaTime : this.simManager.dt;
            //var currEulers = this.geometry.localRotation.eulerAngles;
            this.geometry.localRotation *= Quaternion.Euler(0, 0, 360f * dt / this.dayPeriod);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.GetComponentInParent<PlayerController>() != null)
        {
            this.gameLogic.LoseGameAsync();
        }
    }

    void UpdateScale()
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
