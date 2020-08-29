using UnityEngine;

public class ScanEffect : RadiusEffect
{
    [HideInInspector]
    public float scanned = 0;
    LineRenderer scanEffect;

    float nextChangeStep = 0;
    Vector3 currentScanPos = Vector3.zero;
    Vector3 targetScanPos = Vector3.zero;
    Vector3 currentScanVelocity = Vector3.zero;

    void Start()
    {
        var scanEffectObject = new GameObject();
        scanEffectObject.transform.SetParent(this.transform);

        this.scanEffect = scanEffectObject.AddComponent<LineRenderer>();
        this.scanEffect.material = new Material(Shader.Find("Sprites/Default"));
        var colorGradient = new Gradient { mode = GradientMode.Blend };
        colorGradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.yellow, 0)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0, 0),
                new GradientAlphaKey(0.5f, 0.25f),
                new GradientAlphaKey(0.5f, 0.95f),
                new GradientAlphaKey(0, 1),
            }
        );
        this.scanEffect.colorGradient = colorGradient;
        this.scanEffect.positionCount = 4;
        this.scanEffect.startWidth = this.scanEffect.endWidth = 0;
        this.scanEffect.enabled = false;
    }

    protected override void Apply(RadiusEffectTarget target, float value, Vector3 direction)
    {
        float newScanned = Mathf.Min(1, this.scanned + value * 0.25f);
        this.scanEffect.enabled = newScanned != this.scanned;

        if (newScanned != this.scanned)
        {
            float radius = this.effector.transform.localScale.x * 0.5f;

            //var scanMoveVector = (Vector3)Vector2.Perpendicular(direction).normalized;
            //var scanStartPos = this.effector.position + scanMoveVector * -radius;
            //var endStartPos = this.effector.position + scanMoveVector * radius;
            //var Vector = this.effector.position + offset - target.transform.position;

            this.currentScanPos = Vector3.SmoothDamp(this.currentScanPos, this.targetScanPos, ref this.currentScanVelocity, 0.1f);
            //var scanTarget = scanStartPos + (endStartPos - scanStartPos) * this.currentScanPos; // Mathf.PingPong(Time.time * 5, 1);
                                                                                                //Quaternion.Euler(0, 0, Time.time * 360f) * Vector3.one * 0.6f;
            var scanVector = (this.effector.position + this.currentScanPos - target.transform.position) + Vector3.back * radius;
            //var relv = (scanVector - this.effector.position).normalized * radius;
            
            this.scanEffect.SetPositions(new[] {
                target.transform.position,
                target.transform.position + scanVector * 0.25f,
                target.transform.position + scanVector * 0.95f,
                target.transform.position + scanVector
            });
            this.scanEffect.endWidth = radius * 0.5f;
            this.scanned = newScanned;

            if (Time.time > this.nextChangeStep)
            {
                this.targetScanPos = Vector3.one * radius * 1.5f * (Random.value - 0.5f);
                this.nextChangeStep = Time.time + Random.value * 0.75f;
            }
        }
    }
};
