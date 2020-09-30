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
    SimManager simManager;

    void Awake()
    {
        this.simManager = FindObjectOfType<SimManager>();
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
            float time = this.simManager == null ? Time.time : this.simManager.time;
            this.geometry.localRotation = Quaternion.Euler(0, 0, this.rotationOffset + 360f * time / this.dayPeriod);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.GetComponentInParent<PlayerController>() != null)
        {
            this.gameLogic.LoseGame();
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
