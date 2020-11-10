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

    public Color color = Color.white;
    public float uvStretch = 1f;

    public MeshFilter bakeMeshFilter;

    MaterialPropertyBlock propertyBlock;

    void Awake()
    {
        if(this.lineRenderer == null)
        {
            this.lineRenderer = this.GetComponent<LineRenderer>();
        }
        this.lineRenderer.useWorldSpace = false;

        this.propertyBlock = new MaterialPropertyBlock();
    }

    void Start()
    {
        this.UpdateCircle();
    }

    void Update()
    {
        if(this.screenSpaceWidth)
        {
            float size = GetWorldFromScreenSpaceSize(this.pixelWidth);
            this.lineRenderer.startWidth = this.lineRenderer.endWidth = size;


            float ratio = (float)this.lineRenderer.sharedMaterial.mainTexture.height / this.lineRenderer.sharedMaterial.mainTexture.width;

            if(this.lineRenderer.HasPropertyBlock())
            {
                this.lineRenderer.GetPropertyBlock(this.propertyBlock);
            }
            this.propertyBlock.SetVector("_UVScaling", new Vector2(uvStretch * ratio / size, 1));
            this.propertyBlock.SetColor("_BaseColor", this.color);
            this.lineRenderer.SetPropertyBlock(this.propertyBlock);
        }
    }

    public void UpdateCircle()
    {
        this.lineRenderer.loop = this.degrees == 360f;
        int vertexNumber = Mathf.Max(1, (int)(this.degrees * this.quality));
        // If we are not looping then we need to step further each vertex to meet the
        // degrees requirement (fencepost problem)
        float dAngle = this.degrees * Mathf.Deg2Rad / (vertexNumber - (this.lineRenderer.loop? 0 : 1));
        float startAngle = (360f - this.degrees) * Mathf.Deg2Rad / 2f;
        this.lineRenderer.positionCount = vertexNumber;

        for (int i = 0; i < vertexNumber; i++)
        {
            var pos = new Vector3(Mathf.Cos(startAngle + dAngle * i), Mathf.Sin(startAngle + dAngle * i), 0) * this.radius;
            this.lineRenderer.SetPosition(i, pos);
        }

        if(this.bakeMeshFilter != null)
        {
            this.lineRenderer.BakeMesh(this.bakeMeshFilter.mesh);
            this.lineRenderer.enabled = false;
        }
    }

    static float GetWorldFromScreenSpaceSize(float pixels)
    {
        return Camera.main.orthographicSize * pixels / Screen.width;
    }
}
