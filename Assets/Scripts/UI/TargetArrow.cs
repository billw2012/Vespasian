using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

[RequireComponent(typeof(TextMeshProUGUI))]
public class TargetArrow : MonoBehaviour
{
    public GameObject target;

    TextMeshProUGUI image;

    void Start()
    {
        if(this.target == null)
        {
            this.target = GameObject.Find("Target");
        }
        Assert.IsNotNull(this.target);
        this.image = this.GetComponent<TextMeshProUGUI>();
        Assert.IsNotNull(this.image);
    }

    // Update is called once per frame
    void Update()
    {
        //var targetScreenPos = (Vector2)Camera.main.WorldToScreenPoint(this.target.transform.position);
        var targetCanvasPosition = this.image.canvas.WorldToCanvasPosition(this.target.transform.position);

        var canvasSafeArea = this.image.canvas.ScreenToCanvasRect(Screen.safeArea);
        if (!canvasSafeArea.Contains(targetCanvasPosition))
            //Screen.safeArea.Contains(targetScreenPos))
        {
            var rectTransform = this.GetComponent<RectTransform>();
            var clampArea = new Rect(
                canvasSafeArea.x - rectTransform.rect.x,
                canvasSafeArea.y - rectTransform.rect.y,
                canvasSafeArea.width - rectTransform.rect.width,
                canvasSafeArea.height - rectTransform.rect.height
            );
            var clampedTargetScreenPos = clampArea.IntersectionWithRayFromCenter(targetCanvasPosition);
            rectTransform.anchoredPosition = clampedTargetScreenPos;
            rectTransform.rotation = Quaternion.FromToRotation(Vector3.right, clampedTargetScreenPos - clampArea.center);
            this.image.enabled = true;
        }
        else
        {
            this.image.enabled = false;
        }
    }
}
