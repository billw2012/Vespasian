using UnityEngine;
using UnityEngine.Assertions;

public class PlanetLogic : MonoBehaviour
{
    [Tooltip("Planet Radius (use this instead of scaling)"), Range(0, 10)]
    public float radius = 0.5f;

    [Tooltip("Time to complete a full axial rotation (0 = no rotation)"), Range(-100, 100)]
    public float dayPeriod = 0;

    public bool ringEnabled = false;
    public float ringRadiusFactor = 1.5f;
    public float ringWidthFactor = 0.5f;

    public Transform geometry;

    public GameLogic gameLogic;

    void UpdateScale()
    {
        this.geometry.localScale = Vector3.one * this.radius;
        // Make sure to update auto mass
        var gravity = this.GetComponent<GravitySource>();
        if(gravity != null)
        {
            gravity.RefreshValidate();
        }
    }

    void OnValidate()
    {
        this.UpdateScale();
    }

    void Start()
    {
        this.UpdateScale();
    }

    void Update()
    {
        if (this.dayPeriod != 0)
        {
            var rot = this.geometry.rotation.eulerAngles;
            this.geometry.rotation = Quaternion.Euler(rot + 360f * Vector3.forward * Time.deltaTime / this.dayPeriod);
            // this.position.localRotation  this.dayPeriod * simTime;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.GetComponentInParent<PlayerLogic>() != null)
        {
            this.gameLogic.LoseGame();
        }
    }
}
