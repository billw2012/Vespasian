using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

public class RayShadow : MonoBehaviour
{
    public float shadowLengthScale = 30.0f;
    public MeshFilter geometry;
    public Vector2 shadowCasterSize = new Vector2(0.5f, 0.5f);
    [Range(0.1f, 2)]
    public float shadowFadeInFactor = 1f;
    [Range(0, 1)]
    public float shadowIntensity = 0.5f;

    struct LightAndShadow
    {
        public Transform light;
        public GameObject shadow;
        public LineRenderer lineRenderer;
    }

    List<LightAndShadow> rays;

    Vector3 localExtents;
    float shadowLength;

    // Start is called before the first frame update
    void Start()
    {
        if (this.geometry == null)
        {
            this.geometry = this.GetComponent<MeshFilter>();
        }

        // We need the extents to decide the length of the shadow
        var relativeMatrix = this.transform.worldToLocalMatrix * this.geometry.transform.localToWorldMatrix;

        this.localExtents = Vector2.Scale(this.shadowCasterSize, relativeMatrix.lossyScale);
        this.shadowLength = this.localExtents.magnitude * this.shadowLengthScale;

        this.Refresh();
    }

    void Refresh()
    {
        float darkestPoint = this.localExtents.magnitude * this.shadowFadeInFactor / this.shadowLength;

        if(this.rays != null)
        {
            foreach(var r in this.rays)
            {
                Destroy(r.shadow);
            }
        }

        var suns = FindObjectsOfType<StarLogic>();
        this.rays = suns.Select(light =>
            {
                var shadow = new GameObject($"Shadow ({light.name})");
                shadow.transform.SetParent(this.geometry.transform, worldPositionStays: false);
                shadow.hideFlags = HideFlags.NotEditable | HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;

                var lineRenderer = shadow.AddComponent<LineRenderer>();
                lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
                var colorGradient = new Gradient { mode = GradientMode.Blend };
                colorGradient.SetKeys(
                    new GradientColorKey[] {
                        new GradientColorKey(Color.black, 0),
                    },
                    new GradientAlphaKey[] {
                        new GradientAlphaKey(0, 0),
                        new GradientAlphaKey(this.shadowIntensity, darkestPoint),
                        new GradientAlphaKey(0, 1),
                    }
                );
                lineRenderer.colorGradient = colorGradient;
                lineRenderer.positionCount = 3;
                var orbitComponent = light.GetComponent<Orbit>();
                return new LightAndShadow
                {
                    light = orbitComponent.position,
                    shadow = shadow,
                    lineRenderer = lineRenderer,
                };
            })
            .ToList();


    }

    // Update is called once per frame
    void Update()
    {
        // Remove 
        if(this.rays.Any(r => r.light == null))
        {
            this.Refresh();
        }

        var heightOffset = Vector3.back * 10f;
        var rayStartPos = this.geometry.transform.position + heightOffset;
        foreach (var ray in this.rays)
        {
            // Set start and end
            if (ray.lineRenderer.GetPosition(0) != rayStartPos)
            {
                ray.lineRenderer.SetPosition(0, rayStartPos);
            }
            var lightPos = ray.light.transform.position;
            var lightRay = (this.geometry.transform.position - lightPos).normalized;

            var rayMid = rayStartPos + lightRay * this.localExtents.magnitude * this.shadowFadeInFactor;
            if (ray.lineRenderer.GetPosition(1) != rayMid)
            {
                ray.lineRenderer.SetPosition(1, rayMid);
            }

            var rayEnd = rayStartPos + lightRay * this.shadowLength;
            if (ray.lineRenderer.GetPosition(2) != rayEnd)
            {
                ray.lineRenderer.SetPosition(2, rayEnd);
            }

            // Determine the width of the shadow we should cast:
            float width;
            if(this.localExtents.x == this.localExtents.y)
            {
                width = this.localExtents.x;
            }
            else
            {
                // Project the x and y axis of the scaled oriented bounding box onto the vector perpendicular to 
                // the direction of the light.
                var perpVec = Vector3.Cross(lightRay, Vector3.forward).normalized;
            
                var xAxis = this.transform.TransformDirection(this.localExtents.x00());
                var yAxis = this.transform.TransformDirection(this.localExtents._0y0());
                width = Mathf.Max(
                        Vector3.Project(xAxis, perpVec).magnitude,
                        Vector3.Project(yAxis, perpVec).magnitude
                    );
            }

            ray.lineRenderer.startWidth = width;
            ray.lineRenderer.endWidth = width * 0.5f;

        }
    }
}
