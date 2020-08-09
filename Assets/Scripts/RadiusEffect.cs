using UnityEngine;

public abstract class RadiusEffect : MonoBehaviour
{
    [Tooltip("Radius at which effect starts, as a multiplier of the actual radius"), Range(0, 10)]
    public float maxRadiusMultiplier = 1.0f;
    [Tooltip("Strength of the effect"), Range(0, 100)]
    public float effectFactor = 1.0f;
    [Tooltip("Object to use as the effect source")]
    public Transform effector;

    // Update is called once per frame
    void Update()
    {
        // TODO: check for player proximity to deliver fuel/damage/aero breaking/etc.
        var radius = this.effector.transform.localScale.x;
        var planetPlayerVec = GameLogic.Instance.player.transform.position - this.effector.transform.position;
        var playerHeight = planetPlayerVec.magnitude;
        var heightRatio = 1 - (playerHeight - radius) / (radius * this.maxRadiusMultiplier);
        this.Apply(Mathf.Max(0, Time.deltaTime * this.effectFactor * heightRatio), planetPlayerVec.normalized);
    }

    protected virtual void Apply(float value, Vector3 direction) { }
}
