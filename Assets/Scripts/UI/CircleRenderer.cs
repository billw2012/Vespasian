using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CircleRenderer : MonoBehaviour
{
    public LineRenderer lineRenderer;
    public float radius;
    public bool screenSpaceWidth;
    [Range(0, 100)]
    public float pixelWidth;
    [Range(0, 1)]
    public float quality = 1;
    [Range(0, 360)]
    public float degrees = 360f;

    void Awake()
    {
        if(this.lineRenderer == null)
        {
            this.lineRenderer = this.GetComponent<LineRenderer>();
        }
        this.lineRenderer.useWorldSpace = false;
    }

    void Start()
    {
        this.UpdateCircle();
    }

    void Update()
    {
        if(this.screenSpaceWidth)
        {
            this.lineRenderer.startWidth = this.lineRenderer.endWidth = GetWorldFromScreenSpaceSize(this.pixelWidth);
        }
    }

    public void UpdateCircle()
    {
        int vertexNumber = Mathf.Max(1, (int)(this.degrees * this.quality));
        float angle = this.degrees * Mathf.Deg2Rad / vertexNumber;
        float startAngle = (360f - this.degrees) * Mathf.Deg2Rad / 2f;
        this.lineRenderer.positionCount = vertexNumber + 1;

        for (int i = 0; i <= vertexNumber; i++)
        {
            var pos = new Vector3(Mathf.Cos(startAngle + angle * i), Mathf.Sin(startAngle + angle * i), 0) * this.radius;
            this.lineRenderer.SetPosition(i, pos);
        }
    }

    static float GetWorldFromScreenSpaceSize(float pixels)
    {
        return Camera.main.orthographicSize * pixels / Screen.width;
    }
}
