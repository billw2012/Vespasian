using UnityEngine;

[CreateAssetMenu]
public class GameConstants : ScriptableObject
{
    public float GravitationalConstant = 1.0f;
    [Tooltip("How much to reduce the gravitational influence of sources outside the current primary hierarchy"), Range(1, 5)]
    public float GravitationalRescaling = 1.0f;

    public float GameSpeedBase = 1f;
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

    private static float GetWorldFromScreenSpaceSize(float pixels)
    {
        return Camera.main.orthographicSize * pixels / Screen.width;
    }
}