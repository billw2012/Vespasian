using UnityEngine;

public class ScanEffect : RadiusEffect
{
    [HideInInspector]
    public float scanned = 0;

    public bool scanning = false;

    public bool fullyScanned => this.scanned >= 1;

    protected override void Apply(RadiusEffectTarget target, float value, float heightRatio, Vector3 direction)
    {
        this.scanning = !this.fullyScanned && value > 0;
        if (this.scanning)
        {
            if (target.GetComponentInChildren<Scanner>().MarkTargetActive(this.effector))
            {
                this.scanned = Mathf.Clamp(this.scanned + value * 0.25f, 0, 1);
            }
        }
        else
        {
            target.GetComponentInChildren<Scanner>().MarkTargetInactive(this.effector);
        }
    }
};
