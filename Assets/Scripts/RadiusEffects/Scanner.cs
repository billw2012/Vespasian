using Pixelplacement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Scanner : MonoBehaviour
{
    public ParticleSystem laserScanner;

    Scannable target = null;

    // Start is called before the first frame update
    void Start()
    {
        this.laserScanner.gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        // Try to find a target to scan
        if (this.target == null)
            this.target = EffectSource.GetNearestEffectSource<Scannable>(this.transform);

        //Debug.Log($"Scanner target: {this.target}");

        if (this.target != null)
        {
            // Progress scan of this target
            this.target.Scan(this);

            // End scan of this target if it's fully scanned
            if (target.IsEmpty())
            {
                this.target = null;
                // Add score?
            }

            // Stop scan if target is too far
            if (this.target != null)
                if (!target.IsInEffectRange(this.transform))
                    this.target = null;

            // Update effects
            if (this.target != null)
            {
                this.laserScanner.gameObject.SetActive(true);
                var vectorToTarget = this.target.effectSourceTransform.position - this.transform.position;
                this.laserScanner.transform.rotation =
                    Quaternion.Euler(0, 0, Vector2.SignedAngle(Vector2.right, vectorToTarget)) *
                    Quaternion.Euler(0, 45f, 0)
                    ;
                float targetWidth = this.target.effectSourceTransform.gameObject.GetFullMeshRendererBounds().extents.magnitude * 2f;
                this.laserScanner.transform.localScale = new Vector3(vectorToTarget.magnitude * 1.5f, targetWidth, 1);
            }
        }
        else
        {
            this.laserScanner.gameObject.SetActive(false);
        }
    }
}
