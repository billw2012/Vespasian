using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RayShadow : MonoBehaviour
{
    struct LightAndShadow
    {
        public Light light;
        public GameObject shadow;
        public LineRenderer lineRenderer;
    }

    List<LightAndShadow> rays;

    public float shadowLengths = 1000;

    // Start is called before the first frame update
    void Start()
    {
        var shadowStartWidth = this.transform.localScale.x;
        var sun = GameObject.Find("SunMain").GetComponent<Light>();
        this.rays = 
            // FindObjectsOfType(typeof(Light))
            new [] { sun }
            .Cast<Light>().Select(light => {
                var shadow = new GameObject();
                var lineRenderer = shadow.AddComponent<LineRenderer>();
                lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
                lineRenderer.startWidth = shadowStartWidth;
                lineRenderer.endWidth = shadowStartWidth;
                lineRenderer.startColor = new Color(0, 0, 0, 0.5f);
                lineRenderer.endColor = new Color(0, 0, 0, 1.0f);
                lineRenderer.positionCount = 2;
                return new LightAndShadow
                {
                    light = light,
                    shadow = shadow,
                    lineRenderer = lineRenderer,
                };
            })
            .ToList();
    }

    // Update is called once per frame
    void Update()
    {
        var planetPos = this.transform.position;
        foreach (var ray in this.rays)
        {
            if (ray.lineRenderer.GetPosition(0) != planetPos)
            {
                ray.lineRenderer.SetPosition(0, planetPos);
            }
            var lightPos = ray.light.transform.position;
            var rayEnd = planetPos + (planetPos - lightPos).normalized * this.shadowLengths;
            if (ray.lineRenderer.GetPosition(1) != rayEnd)
            {
                ray.lineRenderer.SetPosition(1, rayEnd);
            }
        }
    }
}
