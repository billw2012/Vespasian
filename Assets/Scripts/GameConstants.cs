using UnityEngine;

[CreateAssetMenu]
public class GameConstants : ScriptableObject
{
    public float GravitationalConstant = 5.0f;

    public float SimStepDt = 0.05f;
    public float SimDistanceLimit = 100f;

    public float FuelUse = 0.1f;

    // Modifiers of thrust of engine groups
    public float ThrustForward = 1.0f;
    public float ThrustRight = 0.3f;

    public float MaxLaunchVelocity = 3.0f;

    public float OrbitLineWidthPixels = 5.0f;

    public float SimPathLineWidthPixels = 5.0f;

    [HideInInspector]
    public float OrbitLineWidth => GetWorldFromScreenSpaceSize(this.OrbitLineWidthPixels);

    [HideInInspector]
    public float SimLineWidth => GetWorldFromScreenSpaceSize(this.SimPathLineWidthPixels);

    static float GetWorldFromScreenSpaceSize(float pixels)
    {
        return Camera.main.orthographicSize * pixels / Screen.width;
    }
}