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
        var targetScreenPos = (Vector2)Camera.main.WorldToScreenPoint(this.target.transform.position);

        var image = this.GetComponent<TextMeshProUGUI>();
        if (!Screen.safeArea.Contains(targetScreenPos))
        {
            var rectTransform = this.GetComponent<RectTransform>();
            var clampArea = new Rect(
                Screen.safeArea.x - rectTransform.rect.x,
                Screen.safeArea.y - rectTransform.rect.y,
                Screen.safeArea.width - rectTransform.rect.width,
                Screen.safeArea.height - rectTransform.rect.height
            );
            var clampedTargetScreenPos = clampArea.IntersectionWithRayFromCenter(targetScreenPos);
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
