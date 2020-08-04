using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TargetArrow : MonoBehaviour
{
    public GameObject target;

    // Update is called once per frame
    void Update()
    {
        Debug.Assert(this.target != null);
        //var targetScreenPos = (Vector2)Camera.main.WorldToScreenPoint(this.target.transform.position);

        var image = this.GetComponent<TextMeshProUGUI>();
        var targetCanvasPosition = image.canvas.WorldToCanvasPosition(this.target.transform.position);

        var canvasSafeArea = image.canvas.ScreenToCanvasRect(Screen.safeArea);
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
            image.enabled = true;
        }
        else
        {
            image.enabled = false;
        }
    }
}
