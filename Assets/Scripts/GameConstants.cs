using UnityEngine;

public class GameConstants : MonoBehaviour {

    public static GameConstants Instance;

    public float AccelerationCoefficient = 1.0f;

    public float GlobalCoefficient = 5.0f;

    public int MinPhysicsSteps = 1;
    public int MaxPhysicsSteps = 1;

    public float FuelUse = 0.1f;

    public float MaxLaunchVelocity = 3.0f;

    void Start()
    {
        Instance = this;
    }
}