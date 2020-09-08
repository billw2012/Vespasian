using UnityEngine;

public class ScanEffect : RadiusEffect
{
    [HideInInspector]
    public float scanned = 0;
    LineRenderer lineRenderer;

    // Prefab for laser sprite
    public GameObject laserSpritePrefab;
    GameObject laserSpriteObject;
    SpriteRenderer laserSpriteRenderer;

    float scanBeamAngle = -1;       // Animates between -1 and 1
    float scanBeamDirection = 1;    // Alternates between -1 or 1
    const float scanAngleSpeed = 12.0f;

    void Start()
    {
        // Create laser sprite object
        this.laserSpriteObject = Instantiate(this.laserSpritePrefab);
        this.laserSpriteRenderer = laserSpriteObject.GetComponentInChildren<SpriteRenderer>();

        // Create line renderer
        var scanEffectObject = new GameObject();
        scanEffectObject.transform.SetParent(this.transform);

        this.lineRenderer = scanEffectObject.AddComponent<LineRenderer>();
        this.lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        /*
        var colorGradient = new Gradient { mode = GradientMode.Blend };
        colorGradient.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(Color.red, 0)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0, 0),
                new GradientAlphaKey(0.5f, 0.25f),
                new GradientAlphaKey(0.5f, 0.95f),
                new GradientAlphaKey(0, 1),
            }
        );
        this.scanEffect.colorGradient = colorGradient;
        */
        this.lineRenderer.startColor = new Color(1, 0, 0, 0.6f);
        this.lineRenderer.endColor = new Color(1, 0, 0, 0.6f);
        this.lineRenderer.positionCount = 2;
        this.lineRenderer.startWidth = this.lineRenderer.endWidth = 0.1f;
        this.lineRenderer.enabled = false;
    }

    protected override void Apply(RadiusEffectTarget target, float value, float heightRatio, Vector3 direction)
    {
        float newScanned = Mathf.Clamp(this.scanned + value * 0.25f, 0, 1000);
        this.lineRenderer.enabled = newScanned != this.scanned;

        if (newScanned != this.scanned)
        {
            // Animate angle
            this.scanBeamAngle = Mathf.Clamp(this.scanBeamAngle + ScanEffect.scanAngleSpeed * Time.deltaTime * this.scanBeamDirection, -1, 1);
            if (this.scanBeamDirection > 0)
            {
                if (this.scanBeamAngle >= 1.0f)
                    this.scanBeamDirection = -1.0f;
            }
            else
            {
                if (this.scanBeamAngle <= -1.0f)
                    this.scanBeamDirection = 1.0f;
            }

            // Calculate direction vectors
            // Vector from planet to ship
            Vector3 diff = this.effector.transform.position - target.transform.position;
            Vector3 dir = Vector3.Normalize(diff);
            // Vector orthogonal ot the one above
            Vector3 norm = Vector3.Cross(dir, Vector3.forward);
            
            // Draw line            
            float planetRadius = this.effector.transform.localScale.x * 0.5f;
            Vector3[] positions =
            {
                target.transform.position,
                this.effector.transform.position + planetRadius*norm*this.scanBeamAngle
            };
            this.lineRenderer.SetPositions(positions);

            // Rotate sprite accordingly
            this.laserSpriteRenderer.enabled = true;
            this.laserSpriteObject.transform.position = 0.5f * (target.transform.position + this.effector.transform.position);
            this.laserSpriteObject.transform.LookAt(this.effector.transform);
            this.laserSpriteObject.transform.localScale = new Vector3(2*planetRadius, 2*planetRadius, diff.magnitude);

            this.scanned = newScanned;
        }
        else
        {
            this.laserSpriteRenderer.enabled = false;
        }
    }
};
