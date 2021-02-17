using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RadarActiveIndicatorManagerComponent : MonoBehaviour
{
    [SerializeField]
    private GameObject indicatorPrefab = null;

    private List<RadarActiveTargetIndicatorComponent> indicators = new List<RadarActiveTargetIndicatorComponent>();

    private Canvas canvas = null;

    [SerializeField]
    private RectTransform uiTransform = null;

    private void Awake()
    {
        this.canvas = GetComponent<Canvas>();
    }

    private const float indicatorLifetime = 5.0f;

    // Update is called once per frame
    private void Update()
    {
        // Remove destroyed indicators from the list
        this.indicators.RemoveAll(ind => ind == null);

        foreach (var indicator in this.indicators)
        {
            indicator.timer += Time.deltaTime;

            if (indicator.timer >= indicatorLifetime)
            {
                Destroy(indicator.gameObject);
            }
            else
            {
                this.UpdateIndicatorPosition(indicator);

                float alpha = 1.0f - indicator.timer / 5.0f;
                foreach (var img in indicator.images)
                {
                    var colorOld = img.color;
                    colorOld.a = alpha;
                    img.color = colorOld;
                }
            }
        }
    }

    public void CreateIndicator(Vector3 worldPos, Vector3 velocity)
    {
        var indicatorGameObject = GameObject.Instantiate(this.indicatorPrefab, this.uiTransform);
        var indicatorComponent = indicatorGameObject.GetComponent<RadarActiveTargetIndicatorComponent>();
        indicatorComponent.worldPosition = worldPos;
        indicatorComponent.velocity = velocity;
        indicatorComponent.images = indicatorComponent.GetComponentsInChildren<Image>();
        indicatorComponent.timer = 0;

        this.indicators.Add(indicatorComponent);

        this.InitIndicator(indicatorComponent);
    }

    private void InitIndicator(RadarActiveTargetIndicatorComponent indicator)
    {
        // Initialize pointer direction
        float angle = Mathf.Rad2Deg*Mathf.Atan2(indicator.velocity.y, indicator.velocity.x) - 90.0f;
        indicator.pointerTransform.localRotation = Quaternion.Euler(new Vector3(0, 0, angle));
        this.UpdateIndicatorPosition(indicator);
    }

    private void UpdateIndicatorPosition(RadarActiveTargetIndicatorComponent indicator)
    {
        Vector3 canvasPos = this.canvas.WorldToCanvasPosition(indicator.worldPosition);
        RectTransform indicatorTransform = indicator.GetComponent<RectTransform>();
        indicatorTransform.anchoredPosition = canvasPos;
    }
}
