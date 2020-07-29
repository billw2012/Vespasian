using UnityEngine;

public class GameConstants : MonoBehaviour {

    public static GameConstants Instance;

    public float AccelerationCoefficient = 1.0f;

    public float GlobalCoefficient = 0.1f;

    public int PhysicsSteps = 100;

    void Start()
    {
        Instance = this;
    }
}