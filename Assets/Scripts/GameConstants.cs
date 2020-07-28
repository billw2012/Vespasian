using UnityEngine;

public class GameConstants : MonoBehaviour {

    public static GameConstants Instance;

    public float AccelerationCoefficient = 1.0f;

    void Start()
    {
        Instance = this;
    }
}