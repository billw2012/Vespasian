using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

public class RayShadow : MonoBehaviour
{
    public float shadowLengthScale = 30.0f;
    public MeshFilter geometry;

    struct LightAndShadow
    {
        public Transform light;
        public GameObject shadow;
        public LineRenderer lineRenderer;
    }

    List<LightAndShadow> rays;

    // Start is called before the first frame update
    void Start()
    {
        if(this.geometry == null)
        {
            this.geometry = this.GetComponent<MeshFilter>();
            Assert.IsNotNull(this.geometry);
        }

        var suns = GameObject.FindObjectsOfType<SunLogic>();
        this.rays = suns.Select(light =>
            {
                var shadow = new GameObject();
                shadow.transform.SetParent(this.geometry.transform);

                var lineRenderer = shadow.AddComponent<LineRenderer>();
                lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
                var colorGradient = new Gradient { mode = GradientMode.Blend };
                colorGradient.SetKeys(
                    new GradientColorKey[] {
                        new GradientColorKey(Color.black, 0),
                    },
                    new GradientAlphaKey[] {
                        new GradientAlphaKey(0, 0),
                        new GradientAlphaKey(0.5f, 0.1f),
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
        var ourPos = this.geometry.transform.position;
        foreach (var ray in this.rays)
        {
            // We need the extents to decide the length of the shadow
            var localExtents = Vector3.Scale(this.geometry.mesh.bounds.extents, this.geometry.transform.localScale);
            float shadowLength = localExtents.magnitude * this.shadowLengthScale;

            // Set start and end
            if (ray.lineRenderer.GetPosition(0) != ourPos)
            {
                ray.lineRenderer.SetPosition(0, ourPos);
            }
            var lightPos = ray.light.transform.position;
            var lightRay = (ourPos - lightPos).normalized;

            var rayMid = ourPos + lightRay * localExtents.magnitude;
            if (ray.lineRenderer.GetPosition(1) != rayMid)
            {
                ray.lineRenderer.SetPosition(1, rayMid);
            }

            var rayEnd = ourPos + lightRay * shadowLength;
            if (ray.lineRenderer.GetPosition(2) != rayEnd)
            {
                ray.lineRenderer.SetPosition(2, rayEnd);
            }

            // Determine the width of the shadow we should cast:
            float width;
            if(localExtents.x == localExtents.y)
            {
                width = localExtents.x * 2;
            }
            else
            {
                // Project the x and y axis of the scaled oriented bounding box onto the vector perpendicular to 
                // the direction of the light.
                var perpVec = Vector3.Cross(lightRay, Vector3.forward).normalized;
            
                var xAxis = this.transform.TransformDirection(localExtents.x00());
                var yAxis = this.transform.TransformDirection(localExtents._0y0());
                width = Mathf.Max(
                        Vector3.Project(xAxis, perpVec).magnitude,
                        Vector3.Project(yAxis, perpVec).magnitude
                    ) * 2;
            }

            ray.lineRenderer.startWidth = width;
            ray.lineRenderer.endWidth = width * 0.5f;

        }
    }
}
