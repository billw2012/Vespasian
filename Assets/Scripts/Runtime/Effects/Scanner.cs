using Pixelplacement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Scanner : MonoBehaviour
{
    public ParticleSystem laserScanner;
    public float scanRate = 0.1f;

    Scannable target = null;

    void Start()
    {
        this.laserScanner.gameObject.SetActive(false);
    }

    void Update()
    {
        // Try to find a target to scan
        if (this.target == null)
            this.target = EffectSource.GetNearest<Scannable>(this.transform);

        //Debug.Log($"Scanner target: {this.target}");

        if (this.target != null)
        {
            // Progress scan of this target
            this.target.Scan(this);

            // End scan of this target if it's fully scanned or too far away
            if (this.target.IsEmpty() || !this.target.IsInRange(this.transform))
            {
                this.target = null;
            }
            else // Update effects
            {
                this.laserScanner.gameObject.SetActive(true);
                var vectorToTarget = this.target.originTransform.position - this.transform.position;
                this.laserScanner.transform.rotation =
                    Quaternion.Euler(0, 0, Vector2.SignedAngle(Vector2.right, vectorToTarget)) *
                    Quaternion.Euler(0, 45f, 0)
                    ;
                float targetWidth = this.target.originTransform.gameObject.GetFullMeshRendererBounds().extents.magnitude * 2f;
                this.laserScanner.transform.localScale = new Vector3(vectorToTarget.magnitude * 1.5f, targetWidth, 1);
            }
        }
        else
        {
            this.laserScanner.gameObject.SetActive(false);
        }
    }
}
