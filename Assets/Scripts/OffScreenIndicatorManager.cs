using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;
using UnityEngine.UI;

// Manages off-screen indicators, must be attached to UI

public class OffScreenIndicatorManager : MonoBehaviour
{
    // Indicator prefab
    public GameObject indicatorPrefab;

    // Amount of indicators
    const int nIndicators = 2;

    // List of indicators, indicators are created at start
    List<GameObject> indicators = new List<GameObject>();

    // Cached references to objects in scene
    GravitySource[] gravitySources;
    Canvas canvas;
    public GameObject player;

    // Start is called before the first frame update
    void Start()
    {
        Assert.IsTrue(this.indicatorPrefab != null);
        Assert.IsTrue(this.player != null);

        this.gravitySources = GravitySource.All();

        this.canvas = GetComponentInParent<Canvas>();
        Assert.IsTrue(this.canvas != null);

        // Make indicators in advance
        for (int i = 0; i < nIndicators; i++)
        {
            RectTransform transform = GetComponent<RectTransform>();
            Assert.IsFalse(transform == null, "Transform is null");
            GameObject indicator = Instantiate(this.indicatorPrefab, transform);
            Assert.IsFalse(indicator == null, $"Indicator at iteration {i} is null");
            this.indicators.Add(indicator);
        }
    }

    // Update is called once per frame
    void Update()
    {
        Rect canvasSafeArea = this.canvas.ScreenToCanvasRect(Screen.safeArea);

        // We want to select first N strongest gravity sources
        // and leave only those which are outside of the screen

        // Sort by force metric
        Func<GravitySource, float> forceMetric = src =>
        {
            // Force metric is not full force as it lacks gravity constant and maybe others
            float dist = Vector3.Distance(this.player.transform.position, src.transform.position);
            return src.parameters.mass / dist / dist;
        };
        var sourcesSorted = gravitySources.OrderByDescending(forceMetric);
        
        // Take first N elements
        var sourcesFirstN = sourcesSorted.Take(OffScreenIndicatorManager.nIndicators);

        // Leave only those which are off screen
        Func<GravitySource, bool> isOutsideCanvas = src =>
        {
            var pos = this.canvas.WorldToCanvasPosition(src.transform.position);
            return !canvasSafeArea.Contains(pos);
        };
        var sourcesOutsideCanvas = sourcesFirstN.Where(isOutsideCanvas);

        var gravSourcesToIndicate = sourcesOutsideCanvas.ToArray();

        var prefabRectTransform = this.indicatorPrefab.GetComponent<RectTransform>();
        var clampArea = new Rect(
            canvasSafeArea.x - prefabRectTransform.rect.x,
            canvasSafeArea.y - prefabRectTransform.rect.y,
            canvasSafeArea.width - prefabRectTransform.rect.width,
            canvasSafeArea.height - prefabRectTransform.rect.height
        );

        // Position indicators for planets we chose
        for (int i = 0; i < gravSourcesToIndicate.Length; i++)
        {
            var gravSrc = gravSourcesToIndicate[i];
            var gravSrcCanvasPos = this.canvas.WorldToCanvasPosition(gravSrc.transform.position);
            var clampedPos = clampArea.IntersectionWithRayFromCenter(gravSrcCanvasPos);
            var indicatorRectTransform = this.indicators[i].GetComponent<RectTransform>();
            indicatorRectTransform.anchoredPosition = clampedPos;

            var image = this.indicators[i].GetComponentInChildren<UnityEngine.UI.Image>();
            image.enabled = true;
        }
        
        // Hide all other indicators
        for (int i = gravSourcesToIndicate.Length; i < OffScreenIndicatorManager.nIndicators; i++)
        {
            var image = this.indicators[i].GetComponentInChildren<UnityEngine.UI.Image>();
            image.enabled = false;
        }

        // Debug.Log($"Sources outside of canvas: {sourcesOutsideCanvas.Count()}");
    }
}
