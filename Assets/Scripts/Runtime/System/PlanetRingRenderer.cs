using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Generates and renders a saturn like planetary ring
/// </summary>
public class PlanetRingRenderer : MonoBehaviour
{
    [Range(0, 3)]
    public float innerRadius = 0.75f;
    [Range(0, 3)]
    public float width = 0.5f;
    [Range(0, 1)]
    public float quality = 1;

    public Material material;

    [ColorUsage(showAlpha: true, hdr: true)]
    public Color color = Color.white;
    [ColorUsage(showAlpha: false)]
    public Color emissive = new Color(0.2f, 0.2f, 0.2f);
    [Range(0, 1)]
    public float saturation = 0.3f;
    [Range(0, 1)]
    public float contrast = 1f;
    [Range(0, 1)]
    public float coloration = 0.5f;
    [Range(0, 1)]
    public float patternSelect = 0f;
    [Range(0, 1)]
    public float patternOffset = 0f;

    public GameObject ringParticlePrefab;
    public Renderer planetRenderer;
    
    private GameObject meshObject;
    private GameObject particleObject;

    private void Start()
    {
        this.UpdateRing();
    }

    private void Update()
    {
        if (this.meshObject != null)
        {
            this.meshObject.SetActive(this.planetRenderer.enabled);
        }

        if (this.particleObject != null)
        {
            this.particleObject.SetActive(this.planetRenderer.enabled);
        }
    }

    private void UpdateRing()
    {
        if (this.meshObject == null)
        {
            this.meshObject = new GameObject("Ring", typeof(MeshRenderer), typeof(MeshFilter));
            this.meshObject.transform.SetParent(this.transform, worldPositionStays: false);
            //this.meshObject.hideFlags = HideFlags.NotEditable | HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
            this.meshObject.GetComponent<MeshFilter>().sharedMesh = new Mesh();
        }
        if(this.particleObject == null && this.ringParticlePrefab != null)
        {
            this.particleObject = Instantiate(this.ringParticlePrefab, this.transform);
            this.particleObject.name = "Ring Particles";
            //this.particleObject.hideFlags = HideFlags.NotEditable | HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
        }
        if (this.particleObject != null)
        {
            var pfxShape = this.particleObject.GetComponent<ParticleSystem>().shape;
            pfxShape.radius = this.innerRadius + this.width * 0.9f;
            pfxShape.radiusThickness = 1 - (this.innerRadius + this.width * 0.1f) / pfxShape.radius;
        }

        // Update renderer
        var renderer = this.meshObject.GetComponent<MeshRenderer>();
        renderer.material = this.material;
        var pb = new MaterialPropertyBlock();
        pb.SetColor("_BaseColor", this.color);
        pb.SetFloat("_Saturation", this.saturation);
        pb.SetFloat("_Contrast", this.contrast);
        pb.SetFloat("_Coloration", this.coloration);
        pb.SetColor("_EmissionColor", this.emissive);
        pb.SetFloat("_PatternSelect", this.patternSelect);
        pb.SetFloat("_PatternOffset", this.patternOffset);
        renderer.SetPropertyBlock(pb);

        // Update mesh
        int vertexNumber = Mathf.Max(1, (int)(360 * this.quality));
        float dAngle = 360 * Mathf.Deg2Rad / (vertexNumber - 1);

        var verts = new Vector3[vertexNumber * 2];
        var uvs = new Vector2[vertexNumber * 2];
        var indices = new int[vertexNumber * 6];
        var normals = new Vector3[vertexNumber * 2];
        var tangents = new Vector4[vertexNumber * 2];

        for (int i = 0; i < vertexNumber; i++)
        {
            var pos = new Vector3(Mathf.Cos(dAngle * i), Mathf.Sin(dAngle * i), 0);
            verts[i * 2 + 0] = pos * this.innerRadius;
            verts[i * 2 + 1] = pos * (this.innerRadius + this.width);
            uvs[i * 2 + 0] = new Vector2(i / vertexNumber, 0f);
            uvs[i * 2 + 1] = new Vector2(i / vertexNumber, 1f);
            normals[i * 2 + 0] = normals[i * 2 + 1] = Vector3.back;
            var tangent = Vector2.Perpendicular(pos);
            tangents[i * 2 + 0] = tangents[i * 2 + 1] = new Vector4(pos.x, pos.y, tangent.x, tangent.y);
            if (i < vertexNumber - 1)
            {
                indices[i * 6 + 0] = i * 2 + 0;
                indices[i * 6 + 1] = (i + 1) * 2 + 0;
                indices[i * 6 + 2] = i * 2 + 1;
                indices[i * 6 + 3] = (i + 1) * 2 + 0;
                indices[i * 6 + 4] = (i + 1) * 2 + 1;
                indices[i * 6 + 5] = i * 2 + 1;
            }
        }

        var mesh = this.meshObject.GetComponent<MeshFilter>().sharedMesh;
        mesh.Clear();
        mesh.SetVertices(verts);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(indices, 0);
        mesh.SetNormals(normals);
        mesh.SetTangents(tangents);
    }


    private void OnValidate()
    {
        if (!string.IsNullOrEmpty(this.gameObject.scene.path))
        {
            this.UpdateRing();
        }
    }
}
