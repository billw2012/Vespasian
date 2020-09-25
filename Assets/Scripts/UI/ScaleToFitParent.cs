using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScaleToFitParent : MonoBehaviour
{
    RectTransform ourTransform;
    RectTransform parentTransform;

    void Start()
    {
        this.ourTransform = this.GetComponent<RectTransform>();
        this.parentTransform = this.ourTransform.parent as RectTransform;
    }

    void Update()
    {
        var parentRect = this.parentTransform.GetWorldRect();
        var ourRect = this.ourTransform.GetWorldRect();
        if (parentRect.width == 0 || ourRect.width == 0)
        {
            return;
        }
        float widthRatio = parentRect.width / ourRect.width;
        float heightRatio = parentRect.height / ourRect.height;
        float finalRatio = Mathf.Min(widthRatio, heightRatio);
        this.ourTransform.localScale = Vector3.Max(Vector3.one * 0.001f, this.ourTransform.localScale * finalRatio);
    }
}
