using UnityEngine;

public abstract class RadiusEffect : MonoBehaviour
{
    public float MaxRadiusMultiplier = 1.0f;
    public float EffectFactor = 1.0f;

    // Update is called once per frame
    void Update()
    {
        // TODO: check for player proximity to deliver fuel/damage/aero breaking/etc.
        var radius = this.transform.localScale.x;
        var playerHeight = Vector3.Distance(GameLogic.Instance.player.transform.position, this.transform.position);

        this.Apply(Mathf.Max(0, Time.deltaTime * this.EffectFactor * (1 - (playerHeight - radius) / (radius * this.MaxRadiusMultiplier))));
    }

    protected virtual void Apply(float value) { }
}
