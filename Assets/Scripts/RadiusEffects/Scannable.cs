using UnityEngine;

public class Scannable : EffectSource
{
    [HideInInspector]
    public float scanProgress = 0;

    [HideInInspector]
    public bool scanning = false;

    override public bool IsEmpty()
    {
        return scanProgress >= 1.0f;
    }

    void LateUpdate()
    {
        this.scanning = false;
    }

    // Must be called in update!!
    public void Scan(Scanner scanner)
    {
        float scanAdd = Time.deltaTime * 0.3f;
        this.scanProgress = Mathf.Clamp(this.scanProgress + scanAdd, 0, 1);
        this.scanning = true;
    }
};
