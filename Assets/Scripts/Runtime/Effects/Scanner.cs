using Pixelplacement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Scanner : MonoBehaviour, IUpgradeLogic
{
    public ParticleSystem laserScanner;
    public float scanRate = 0.1f;

    Scannable target = null;

    void Awake()
    {
        this.laserScanner.gameObject.SetActive(false);
    }

    void Update()
    {
        // Try to find a target to scan
        if (this.target == null)
        {
            this.target = EffectSource.GetNearest<Scannable>(this.transform);
        }

        //Debug.Log($"Scanner target: {this.target}");

        if (this.target != null)
        {
            // Progress scan of this target
            this.target.Scan(this);

            // End scan of this target if it's fully scanned or too far away
            if (this.target.IsComplete() || !this.target.IsInRange(this.transform))
            {
                this.target = null;
            }
            else // Update effects
            {
                this.laserScanner.gameObject.SetActive(true);

                (Vector2 vec, float width) ScannerParams(Vector2 target, Vector2 from, float targetRadius)
                {
                    var fromVec = from - target;
                    float angle = Mathf.Min(90f, Mathf.Cos(targetRadius / fromVec.magnitude) * Mathf.Rad2Deg);
                    var A = (Vector2)(Quaternion.AngleAxis(angle, Vector3.forward) * fromVec.normalized) * targetRadius;
                    var B = (Vector2)(Quaternion.AngleAxis(-angle, Vector3.forward) * fromVec.normalized) * targetRadius;
                    return ((A + B) * 0.5f + target - from, (A - B).magnitude);
                }

                var (vec, width) = ScannerParams(this.target.originTransform.position, this.transform.position, this.target.scannedObjectRadius);
                this.SetScanEffect(vec, width);
            }
        }
        else
        {
            this.laserScanner.gameObject.SetActive(false);
        }
    }

    void SetScanEffect(Vector2 vectorToTarget, float targetWidth)
    {
        this.laserScanner.transform.rotation =
            Quaternion.Euler(0, 0, Vector2.SignedAngle(Vector2.right, vectorToTarget)) *
            Quaternion.Euler(0, 45f, 0)
            ;
        this.laserScanner.transform.localScale = new Vector3(vectorToTarget.magnitude, 2.1f * targetWidth, 1);
    }

    #region IUpgradeLogic
    public UpgradeDef upgradeDef { get; private set; }
    public void Install(UpgradeDef upgradeDef) => this.upgradeDef = upgradeDef;
    public async void TestFire()
    {
        this.enabled = false;
        this.laserScanner.gameObject.SetActive(true);
        float startT = Time.time;
        const float duration = 3;
        var anim = Tween.EaseWobble;
        while (Time.time < startT + duration)
        {
            float val = anim.Evaluate((Time.time - startT) / duration);
            var rot = Quaternion.Euler(0, 0, val * 45f);
            this.SetScanEffect(rot * this.transform.up * 4, 4);
            await new WaitForUpdate();
        }
        this.enabled = true;
    }
    public void Uninstall() { }
    #endregion IUpgradeLogic
}
