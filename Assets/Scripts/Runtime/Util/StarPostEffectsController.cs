using UnityEngine;
using UnityEngine.Rendering;

public class StarPostEffectsController : MonoBehaviour
{
    [Header("Coverage")]
    [Tooltip("Multiplier on BodyLogic.radius for coverage calculation — increase to account for glow")]
    public float starRadiusScale = 1f;
    [Tooltip("Coverage fraction below which no effect is applied (t = 0)")]
    [Range(0f, 1f)]
    public float minThreshold = 0.3f;
    [Tooltip("Coverage fraction at which the curve is fully evaluated (t = 1)")]
    [Range(0f, 1f)]
    public float maxThreshold = 1f;

    [Header("Effects")]
    public PostEffectsDriver driver;

    [Header("Debug")]
    public bool debugLog = false;

    private Volume volume;
    private Camera cam;

    private void Start()
    {
        this.volume = GetComponent<Volume>();
        this.cam = Camera.main;
        this.driver.Init(this.volume);
    }

    private void LateUpdate()
    {
        if (this.volume == null || this.cam == null) return;

        var screenRect = this.cam.WorldSpaceRect();
        float screenArea = screenRect.width * screenRect.height;
        if (screenArea <= 0f) return;

        float totalOverlap = 0f;
        foreach (var star in ComponentCache.FindObjectsOfType<StarLogic>())
        {
            var body = star.GetComponent<BodyLogic>();
            if (body == null) continue;
            totalOverlap += MathX.RectCircleOverlap(screenRect, star.transform.position, body.radius * this.starRadiusScale);
        }

        float coverage = Mathf.Clamp01(totalOverlap / screenArea);
        float t = coverage <= this.minThreshold ? 0f
            : Mathf.Clamp01(Mathf.InverseLerp(this.minThreshold, this.maxThreshold, coverage));

        this.driver.Update(t);

        if (this.debugLog)
            Debug.Log($"[StarCoverage] coverage={coverage:F4} t={t:F4}");
    }

    private void OnDestroy()
    {
        this.driver.Reset();
    }
}
