using UnityEngine;

/// <summary>
/// Parallax, wrapping background texture.
/// </summary>
public class Background : MonoBehaviour
{
    public float speedMultiplier = 0.1f;

    // LateUpdate used as we need the ensure the background is always fitted to the final camera size and position
    private Vector2 lastPosition;
    private Vector2 offset;

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;

    private MaterialPropertyBlock pfxPb;

    private float parallaxSpeed => this.speedMultiplier / Camera.main.orthographicSize;


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
        var bl = Camera.main.ScreenToWorldPoint(Vector3.zero);
        var tr = Camera.main.ScreenToWorldPoint(Screen.width * Vector3.right + Screen.height * Vector3.up);
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

        var textureScale = this.meshRenderer.sharedMaterial.GetTextureScale("_MainTex");

        this.pfxPb.SetVector("_MainTex_ST", new Vector4(textureScale.x, textureScale.y, this.offset.x, this.offset.y));

        //this.pfxPb.SetColor("_BaseColor", this.meshRenderer.sharedMaterial.GetColor("_BaseColor").SetA(this.fade));

        this.meshRenderer.SetPropertyBlock(this.pfxPb);
    }

    public void ResetPosition()
    {
        this.lastPosition = this.transform.position;
    }

    public void ApplyPositionOffset(Vector2 offset)
    {
        this.lastPosition += offset;
    }
}
