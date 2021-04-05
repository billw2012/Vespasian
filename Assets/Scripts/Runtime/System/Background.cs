﻿using UnityEngine;

/// <summary>
/// Parallax, wrapping background texture.
/// </summary>
public class Background : MonoBehaviour
{
    [SerializeField]
    private float speedMultiplier = 0.1f;

    public int colorationIndex = -1;
    public float colorValue = 0.7f;
    public float colorAlpha = 0.7f;

    private Color color;
    // LateUpdate used as we need the ensure the background is always fitted to the final camera size and position
    private Vector2 lastPosition;
    private Vector2 offset;

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;

    private MaterialPropertyBlock pfxPb;

    private float parallaxSpeed => this.speedMultiplier / GUILayerManager.MainCamera.orthographicSize;

    private void Start()
    {
        this.lastPosition = this.transform.position;
        this.offset = this.transform.position * -this.parallaxSpeed;

        this.meshFilter = this.GetComponent<MeshFilter>();
        this.meshRenderer = this.GetComponent<MeshRenderer>();
        this.pfxPb = new MaterialPropertyBlock();
    }

    private void LateUpdate()
    {
        // Do this every time, as screen size can change, and its a very cheap calculation
        var bl = GUILayerManager.MainCamera.ScreenToWorldPoint(Vector3.zero);
        var tr = GUILayerManager.MainCamera.ScreenToWorldPoint(Screen.width * Vector3.right + Screen.height * Vector3.up);
        var worldSize = tr - bl;
        float size = this.meshFilter.mesh.bounds.size.x;
        this.transform.localScale = Vector3.one * Mathf.Max(worldSize.x, worldSize.y) / size;

        var movement = (Vector2)this.transform.position - this.lastPosition;
        this.lastPosition = this.transform.position;
        this.offset += movement * -this.parallaxSpeed;
        //this.GetComponent<MeshRenderer>().material.SetTextureOffset("_MainTex", this.offset);

        if (this.meshRenderer.HasPropertyBlock())
        {
            this.meshRenderer.GetPropertyBlock(this.pfxPb);
        }

        var textureScale = this.meshRenderer.sharedMaterial.GetTextureScale("_BaseMap");

        if(this.color != default)
        {
            this.pfxPb.SetColor("_BaseColor", this.color);
        }   
        this.pfxPb.SetVector("_BaseMap_ST", new Vector4(textureScale.x, textureScale.y, this.offset.x, this.offset.y));

        //this.pfxPb.SetColor("_BaseColor", this.meshRenderer.sharedMaterial.GetColor("_BaseColor").SetA(this.fade));

        this.meshRenderer.SetPropertyBlock(this.pfxPb);
    }
    
    public void SetColor(Color color) => this.color = color;

    public void ResetPosition() => this.lastPosition = this.transform.position;

    public void ApplyPositionOffset(Vector2 offset) => this.lastPosition += offset;
}
