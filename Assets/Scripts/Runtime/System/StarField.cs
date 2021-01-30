
using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Describes, generates, and updates a wrapping parallax star field
/// Roughly based on this: http://guidohenkel.com/2018/05/endless_starfield_unity/
/// </summary>
public class StarField : MonoBehaviour
{
    [Range(1, 500)]
    public int maxStars = 100;
    [Range(0.01f, 1f)]
    public float starMinSize = 0.01f;
    [Range(0.01f, 1f)]
    public float starSizeRange = 0.1f;
    [Range(1f, 100f)]
    public float rectSize = 10f;
    [Range(0.01f, 10f)]
    public float speedMultiplier = 0.25f;
    [Range(0.01f, 1f)]
    public float parallaxBase = 1.0f;
    [Range(0.01f, 1f)]
    public float parallaxRange = 1.0f;

    [Range(0f, 1f)]
    public float scaleEffect = 0.25f;

    [Range(0f, 1f)]
    public float animationSpeed = 0.1f;

    public AnimationCurve sizeDistribution;
    public AnimationCurve twinkleAlphaAnim;
    public AnimationCurve rotationAnim;

    public List<Color> colors;

    [NonSerialized]
    public float fade = 1;

    public PinchZoomCamera pinchZoom;

    private ParticleSystem.Particle[] stars;
    private float[] baseSizes;

    private Vector2 lastPosition;
    //Vector2 offset;

    private float parallaxSpeed => this.speedMultiplier / Camera.main.orthographicSize;

    private ParticleSystem pfx;
    private ParticleSystemRenderer pfxRenderer;
    private MaterialPropertyBlock pfxPb;

    private void Awake()
    {
        if(this.colors.Count == 0)
        {
            this.colors.Add(Color.white);
            this.colors.Add(Color.red);
            this.colors.Add(Color.blue);
            this.colors.Add(Color.yellow);
        }

        this.lastPosition = this.transform.position;
        //this.offset = this.transform.position * -this.parallaxSpeed;

        this.stars = new ParticleSystem.Particle[this.maxStars];
        this.baseSizes = new float[this.maxStars];

        float GetRandomSize()
        {
            float sizeScale = this.sizeDistribution != null ? this.sizeDistribution.Evaluate(Random.value) : Random.value;
            return this.starMinSize + sizeScale * this.starSizeRange;
        }

        float halfRectSize = this.rectSize * 0.5f;
        for (int i = 0; i < this.stars.Length; i++)
        {
            // float randSize = Random.Range(this.starSizeRange, this.starSizeRange + 1f);
            // float scaledColor = (true == this.colorize) ? randSize - StarSizeRange : 1f;

            this.stars[i].position = new Vector3(Random.Range(-halfRectSize, halfRectSize), Random.Range(-halfRectSize, halfRectSize), Random.Range(0, this.parallaxRange));
            this.baseSizes[i] = this.stars[i].startSize = GetRandomSize();
            this.stars[i].startColor = this.colors[Random.Range(0, this.colors.Count)];
            this.stars[i].rotation = Random.Range(0f, 360f);
            this.stars[i].axisOfRotation = Vector3.forward;
        }

        this.pfx = this.GetComponent<ParticleSystem>();
        this.pfx.SetParticles(this.stars, this.stars.Length);

        this.pfxPb = new MaterialPropertyBlock();
        this.pfxRenderer = this.GetComponent<ParticleSystemRenderer>();
    }


    private static Color SetAlpha(Color col, float alpha) => new Color(col.r, col.g, col.b, alpha);

    private void LateUpdate()
    {
        float cameraRatio = (float)Camera.main.pixelWidth / Camera.main.pixelHeight;
        float minCameraSize = Mathf.Max(this.pinchZoom.sizeMin * cameraRatio, this.pinchZoom.sizeMin);
        float maxCameraSize = Mathf.Max(this.pinchZoom.sizeMax * cameraRatio, this.pinchZoom.sizeMax);

        float zoomAmount = (Camera.main.orthographicSize - this.pinchZoom.sizeMin) / (this.pinchZoom.sizeMax - this.pinchZoom.sizeMin);

        float adjustedScaleEffect = Mathf.Pow(this.scaleEffect, 1f/3f);
        float desiredSize = Mathf.Lerp(minCameraSize, maxCameraSize, zoomAmount * adjustedScaleEffect + (1 - adjustedScaleEffect));
        //float worldCameraWidth = Camera.main.orthographicSize * 
        // float maxScale = ;

        // Do this every time, as screen size can change, and its a very cheap calculation
        //var bl = Camera.main.ScreenToWorldPoint(Vector3.zero);
        //var tr = Camera.main.ScreenToWorldPoint(Screen.width * Vector3.right + Screen.height * Vector3.up);
        //var worldSize = tr - bl;
        float scale = desiredSize * 2f / this.rectSize;
        this.transform.localScale = Vector3.one * scale;

        var movement = ((Vector2)this.transform.position - this.lastPosition) * -this.parallaxSpeed;
        this.lastPosition = this.transform.position;
        //this.offset += movement * -this.parallaxSpeed;
        //this.GetComponent<MeshRenderer>().material.SetTextureOffset("_BaseMap", this.offset);
        // this.GetComponent<ParticleSystem>().GetParticles(this.stars);

        float halfRectSize = this.rectSize * 0.5f;
        for (int i = 0; i < this.stars.Length; i++)
        {
            var pos = this.stars[i].position + (Vector3)(movement * (this.parallaxBase + 1 - this.stars[i].position.z));

            if (pos.x < - halfRectSize)
            {
                pos.x += this.rectSize;
            }
            else if (pos.x > + halfRectSize)
            {
                pos.x -= this.rectSize;
            }

            if (pos.y < - halfRectSize)
            {
                pos.y += this.rectSize;
            }
            else if (pos.y > + halfRectSize)
            {
                pos.y -= this.rectSize;
            }

            this.stars[i].position = pos;

            if (this.twinkleAlphaAnim != null)
            {
                float size = this.twinkleAlphaAnim.Evaluate(0.1f * this.animationSpeed * Time.time + i * 7.13f);
                this.stars[i].startSize = size * this.baseSizes[i];
                this.stars[i].startColor = SetAlpha(this.stars[i].startColor, size);
            }
            if (this.rotationAnim != null)
            {
                this.stars[i].rotation = 360f * this.rotationAnim.Evaluate(0.3f * this.animationSpeed * Time.time + i * 3.21f);
            }
        }
        this.pfx.SetParticles(this.stars, this.stars.Length);

        if(this.pfxRenderer.HasPropertyBlock())
        {
            this.pfxRenderer.GetPropertyBlock(this.pfxPb);
        }
        
        this.pfxPb.SetColor("_BaseColor", this.pfxRenderer.sharedMaterial.GetColor("_BaseColor").SetA(this.fade));

        this.pfxRenderer.SetPropertyBlock(this.pfxPb);
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
