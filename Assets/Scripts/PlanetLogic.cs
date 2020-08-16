using UnityEngine;

public class PlanetLogic : MonoBehaviour
{
    [Tooltip("Planet Radius (use this instead of scaling)"), Range(0, 10)]
    public float radius = 0.5f;

    public bool ringEnabled = false;
    public float ringRadiusFactor = 1.5f;
    public float ringWidthFactor = 0.5f;

    public Transform geometry;

    void UpdateScale()
    {
        this.geometry.localScale = Vector3.one * this.radius;
    }

    void OnValidate()
    {
        this.UpdateScale();
    }

    // Start is called before the first frame update
    void Start()
    {
        this.UpdateScale();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject == GameLogic.Instance.player)
        {
            GameLogic.Instance.LoseGame();
        }
    }
}
