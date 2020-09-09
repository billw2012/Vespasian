using Pixelplacement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Scanner : MonoBehaviour
{
    public ParticleSystem laserScanner;

    readonly List<Transform> currentTargets = new List<Transform>();

    // Start is called before the first frame update
    void Start()
    {
        this.laserScanner.gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if(this.currentTargets.Any())
        {
            this.laserScanner.gameObject.SetActive(true);
            var vectorToTarget = this.currentTargets[0].position - this.transform.position;
            this.laserScanner.transform.rotation = 
                Quaternion.Euler(0, 0, Vector2.SignedAngle(Vector2.right, vectorToTarget)) *
                Quaternion.Euler(0, 45f, 0)
                ;
            float targetWidth = this.currentTargets[0].gameObject.GetFullMeshRendererBounds().extents.magnitude * 2f;
            this.laserScanner.transform.localScale = new Vector3(vectorToTarget.magnitude * 1.5f, targetWidth, 1);
        }
        else
        {
            this.laserScanner.gameObject.SetActive(false);
        }
    }

    // Return true if the target is currently being scanned
    public bool MarkTargetActive(Transform target)
    {
        if(!this.currentTargets.Contains(target))
        {
            this.currentTargets.Add(target);
        }
        return this.currentTargets.IndexOf(target) == 0;
    }

    public void MarkTargetInactive(Transform target)
    {
        if (this.currentTargets.Contains(target))
        {
            this.currentTargets.Remove(target);
        }
    }
}
