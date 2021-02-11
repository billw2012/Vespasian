using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Pixelplacement;

// Manages off-screen indicators, must be attached to UI

public class OffScreenIndicatorManager : MonoBehaviour
{
    // Indicator prefab
    public GameObject indicatorPrefab;

    // Amount of indicators
    private const int nIndicators = 2;

    // List of indicators, indicators are created at start
    private readonly List<GameObject> indicators = new List<GameObject>();

    private class ObjectiveIndicator
    {
        public PositionalObjective objective;
        public GameObject indicator;
        public bool wasCompleted = false;
        public bool wasOffscreen = true;
        public float angleOffset = 0;
        public float angleOffsetVelocity = 0;
        public float markerGapScale = 0;
    }

    private readonly List<ObjectiveIndicator> objectiveIndicators = new List<ObjectiveIndicator>();

    private PlayerController player;

    // Start is called before the first frame update
    private void Start()
    {
        this.player = ComponentCache.FindObjectOfType<PlayerController>();
        // Make indicators in advance
        for (int i = 0; i < nIndicators; i++)
        {
            this.indicators.Add(ComponentCache.Instantiate(this.indicatorPrefab, this.transform));
        }

        foreach (var objective in ComponentCache.FindObjectsOfType<PositionalObjective>())
        {
            var objectiveUi = ComponentCache.Instantiate(objective.uiAsset, this.transform);
            objectiveUi.GetComponent<ObjectiveIcon>().target = objective;
            this.objectiveIndicators.Add(new ObjectiveIndicator {
                objective = objective,
                indicator = objectiveUi
            });
        }
    }

    // Update is called once per frame
    private void Update()
    {
        var canvas = this.GetComponentInParent<Canvas>();
        var canvasSafeArea = canvas.ScreenToCanvasRect(Screen.safeArea);

        var playerTransform = this.player.transform;
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

        var playerForward = playerTransform.up;
        var playerPosition = playerTransform.position;
        foreach (var objectiveIndicator in this.objectiveIndicators.Where(o => !o.wasCompleted))
        {
            var indicatorTransform = objectiveIndicator.indicator.GetComponent<RectTransform>();
            if (objectiveIndicator.objective.complete || objectiveIndicator.objective.failed)
            {
                int completeObjectives = this.objectiveIndicators.Count(o => o.wasCompleted);
                Tween.AnchoredPosition(indicatorTransform, new Vector2(40, 280 + completeObjectives * 40), 0.25f, 0, Tween.EaseInBack);
                Tween.LocalScale(indicatorTransform, new Vector3(1, 1, 1), 0.25f, 0, Tween.EaseInBack);
                completeObjectives++;
                objectiveIndicator.wasCompleted = true;
            }
            else
            {
                // position of indicator is on radius of objective ahead of player
                var targetPosition = objectiveIndicator.objective.target.position;
                var targetToPlayer = (playerPosition - targetPosition).normalized;
                // Which angle to rotate the indicator out of the players way
                float desiredAngle = Mathf.Sign(Vector2.SignedAngle(targetToPlayer, playerForward)) * 25f;
                objectiveIndicator.angleOffset = Mathf.SmoothDampAngle(objectiveIndicator.angleOffset, desiredAngle, ref objectiveIndicator.angleOffsetVelocity, 1);
                var rotatedRelativePosition = Quaternion.Euler(0, 0, objectiveIndicator.angleOffset) * targetToPlayer;

                var worldPosIndicator = targetPosition + rotatedRelativePosition * objectiveIndicator.objective.radius;

                // Set up the objective marker
                var marker = objectiveIndicator.objective.objectiveMarker;
                marker.transform.localEulerAngles = Vector3.forward * Vector2.SignedAngle(Vector2.right, rotatedRelativePosition);
                // Update the size of the gap in the marker circle so it fits the indicator icon correctly
                var markerCircle = marker.GetComponent<CircleRenderer>();
                // Degrees required is calculated using:
                // s = size of icon in world space
                // d = perimeter of the circle in world space
                //   d = 2 * PI * radius
                // degrees = 360 * (1 - (s / d))

                var indicatorWorld = canvas.GetWorldSpaceRect(indicatorTransform);
                float indicatorSize = Vector3.Distance(indicatorWorld[0], indicatorWorld[2]);
                markerCircle.degrees = 360f * (1 - indicatorSize * 1.5f / (2 * Mathf.PI * objectiveIndicator.objective.radius));
                if (markerCircle.degrees < 180)
                {
                    marker.SetActive(false);
                }
                else
                {
                    marker.SetActive(true);
                    markerCircle.UpdateCircle();
                }

                var canvasPosTarget = canvas.WorldToCanvasPosition(targetPosition);
                var canvasPosIndicator = canvas.WorldToCanvasPosition(worldPosIndicator);
                var canvasIndicatorOffset = canvasPosIndicator - canvasPosTarget;
                var finalIndicatorPos = canvasPosTarget + canvasIndicatorOffset.normalized * canvasIndicatorOffset.magnitude;
                if (canvasSafeArea.Contains(finalIndicatorPos))
                {
                    if (objectiveIndicator.wasOffscreen)
                    {
                        Tween.LocalScale(indicatorTransform, new Vector3(3, 3, 1), 0.25f, 0, Tween.EaseOutBack);
                        objectiveIndicator.wasOffscreen = false;
                    }
                }
                else
                {
                    if (!objectiveIndicator.wasOffscreen)
                    {
                        Tween.LocalScale(indicatorTransform, new Vector3(1, 1, 1), 0.25f, 0, Tween.EaseOutBack);
                        objectiveIndicator.wasOffscreen = true;
                    }
                    finalIndicatorPos = clampArea.IntersectionWithRayFromCenter(finalIndicatorPos);
                }
                indicatorTransform.anchoredPosition = finalIndicatorPos;
            }
        }
    }
}
