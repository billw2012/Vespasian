using UnityEngine;
using UnityEngine.Assertions;

public class PlanetLogic : MonoBehaviour
{
    [Tooltip("Planet Radius (use this instead of scaling)"), Range(0, 10)]
    public float radius = 0.5f;

    public bool ringEnabled = false;
    public float ringRadiusFactor = 1.5f;
    public float ringWidthFactor = 0.5f;

    public Transform geometry;
    public GameLogic gameLogic;

    void UpdateScale()
    {
        this.geometry.localScale = Vector3.one * this.radius;
    }

    void OnValidate()
    {
        Assert.IsNotNull(this.geometry);
        Assert.IsNotNull(this.gameLogic);
        this.UpdateScale();
    }

    void Start()
    {
        this.OnValidate();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.GetComponentInParent<PlayerLogic>() != null)
        {
            this.gameLogic.LoseGame();
        }
    }
}
