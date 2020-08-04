using UnityEngine;

public class PlanetLogic : MonoBehaviour
{
    public bool RingEnabled = false;

    public float RingRadiusFactor = 1.5f;
    public float RingWidthFactor = 0.5f;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject == GameLogic.Instance.player)
        {
            GameLogic.Instance.LoseGame();
        }
    }
}
