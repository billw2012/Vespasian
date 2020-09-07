using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;
using UnityEngine.UI;
using Pixelplacement;

// Manages off-screen indicators, must be attached to UI

public class OffScreenIndicatorManager : MonoBehaviour
{
    // Indicator prefab
    public GameObject indicatorPrefab;

    // Amount of indicators
    const int nIndicators = 2;

    // List of indicators, indicators are created at start
    readonly List<GameObject> indicators = new List<GameObject>();

    class ObjectiveIndicator
    {
        public Objective objective;
        public GameObject indicator;
        public bool wasCompleted;
        public bool wasOffscreen;
    }
    readonly List<ObjectiveIndicator> objectiveIndicators = new List<ObjectiveIndicator>();

    //readonly List<(Objective objective, GameObject indicator)> completeObjectiveIndicators = new List<(Objective objective, GameObject indicator)>();

    // Start is called before the first frame update
    void Start()
    {
        // Make indicators in advance
        for (int i = 0; i < nIndicators; i++)
        {
            this.indicators.Add(Instantiate(this.indicatorPrefab, this.transform));
        }

        foreach (var objective in FindObjectsOfType<Objective>())
        {
            var objectiveUi = Instantiate(objective.uiAsset, this.transform);
            objectiveUi.GetComponent<ObjectiveIcon>().target = objective;
            this.objectiveIndicators.Add(new ObjectiveIndicator {
                objective = objective,
                indicator = objectiveUi, 
                wasCompleted = false,
                wasOffscreen = false
            });
        }
    }

    // Update is called once per frame
    void Update()
    {
        var canvas = this.GetComponentInParent<Canvas>();
        var canvasSafeArea = canvas.ScreenToCanvasRect(Screen.safeArea);

        var playerTransform = FindObjectOfType<PlayerLogic>().transform;
        // We want to select first N strongest gravity sources
        // and leave only those which are outside of the screen

        // Sort by force metric
        float ForceMetric(GravitySource src)
        {
            // Force metric is not full force as it lacks gravity constant and maybe others
            float dist = Vector3.Distance(playerTransform.position, src.transform.position);
            return src.parameters.mass / Mathf.Pow(dist, 2);
        }

        var prefabRectTransform = this.indicatorPrefab.GetComponent<RectTransform>();
        var clampArea = new Rect(
            canvasSafeArea.min + prefabRectTransform.rect.size * prefabRectTransform.pivot,
            canvasSafeArea.size - prefabRectTransform.rect.size
            //.x - prefabRectTransform.rect.x + prefabRectTransform.rect.width * prefabRectTransform.pivot.x,
            //canvasSafeArea.y - prefabRectTransform.rect.y,
            //canvasSafeArea.width - prefabRectTransform.rect.width,
            //canvasSafeArea.height - prefabRectTransform.rect.height
        );

        var indicatorSettings = GravitySource.All().Where(g => g.isActiveAndEnabled)
            .OrderByDescending(ForceMetric)
            .Select(g => canvas.WorldToCanvasPosition(g.transform.position))
            // Leave only those which are off screen
            .Where(pos => !canvasSafeArea.Contains(pos))
            .Select(pos => clampArea.IntersectionWithRayFromCenter(pos))
            .Zip(this.indicators, (indicatorPos, indicator) => (indicatorPos, indicator));

        // Position indicators for planets we chose
        foreach(var (pos, indicator) in indicatorSettings)
        {
            indicator.GetComponent<RectTransform>().anchoredPosition = pos;
            indicator.GetComponentInChildren<UnityEngine.UI.Image>().enabled = true;
        }

        // Hide all other indicators
        foreach(var remainingIndicators in this.indicators
            .Except(indicatorSettings.Select(i => i.indicator)))
        {
            remainingIndicators.GetComponentInChildren<UnityEngine.UI.Image>().enabled = false;
        }

        foreach(var objectiveIndicator in this.objectiveIndicators.Where(o => !o.wasCompleted))
        {
            var canvasPos = (Vector2)canvas.WorldToCanvasPosition(objectiveIndicator.objective.target.position + Vector3.one * objectiveIndicator.objective.radius);

            var indicatorTransform = objectiveIndicator.indicator.GetComponent<RectTransform>();
            if (canvasSafeArea.Contains(canvasPos))
            {
                if(objectiveIndicator.wasOffscreen)
                {
                    Tween.LocalScale(indicatorTransform, new Vector3(3, 3, 1), 0.25f, 0);
                    objectiveIndicator.wasOffscreen = false;
                }
            }
            else
            {
                if (!objectiveIndicator.wasOffscreen)
                {
                    Tween.LocalScale(indicatorTransform, new Vector3(1, 1, 1), 0.25f, 0);
                    objectiveIndicator.wasOffscreen = true;
                }
                canvasPos = clampArea.IntersectionWithRayFromCenter(canvasPos);
            }
            indicatorTransform.anchoredPosition = canvasPos;
            //var clampPos = !canvasSafeArea.Contains(canvasPos) ?
            //    clampArea.IntersectionWithRayFromCenter(canvasPos)
            //    :
            //    canvasPos;
            //indicator.GetComponent<RectTransform>().anchoredPosition = clampPos;

            if(objectiveIndicator.objective.complete)
            {
                int completeObjectives = this.objectiveIndicators.Count(o => o.wasCompleted);
                Tween.AnchoredPosition(indicatorTransform, new Vector2(40, 160 + completeObjectives * 40), 0.25f, 0);
                Tween.LocalScale(indicatorTransform, new Vector3(1, 1, 1), 0.25f, 0);
                completeObjectives++;
                objectiveIndicator.wasCompleted = true;
            }

        }
    }
}
