using UnityEditor;
using UnityEngine;

public class CameraPostEffectRadius : MonoBehaviour
{
    public float radius = 20;
    [Tooltip("Ratio of effect area to screen at which effects will be completely off"), Range(0, 1)]
    public float minCoverage = 0.1f;
    [Tooltip("Ratio of effect area to screen at which effects will be completely on"), Range(0, 1)]
    public float maxCoverage = 0.5f;

    public PostEffectsDriver driver;

    private bool wasInRange = false;

    private void Start()
    {
        this.driver.Init(ComponentCache.FindObjectOfType<UnityEngine.Rendering.Volume>());
    }

    private void Update()
    {
        var screenRect = Camera.main.WorldSpaceRect();
        float coverage = MathX.RectCircleOverlap(screenRect, this.transform.position, this.radius * this.transform.lossyScale.x);
        bool inRange = coverage > 0;
        if (this.wasInRange || inRange)
        {
            float t = Mathf.Clamp01(Mathf.InverseLerp(this.minCoverage, this.maxCoverage, coverage / (screenRect.width * screenRect.height)));
            this.driver.Update(t);
        }
        this.wasInRange = inRange;
    }

    private void OnDestroy()
    {
        this.driver.Reset();
    }


#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Handles.color = UnityEngine.Color.yellow;
        Handles.matrix = this.transform.localToWorldMatrix;
        Handles.DrawWireDisc(Vector3.zero, Vector3.forward, this.radius);
        GUIUtils.Label(Vector3.up * this.radius, "Postfx");
    }
#endif
}
