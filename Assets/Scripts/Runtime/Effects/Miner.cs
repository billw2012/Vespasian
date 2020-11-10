using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Miner : MonoBehaviour
{
    public ParticleSystem miningEffect;

    public float miningRate = 0.01f;

    Mineable target = null;

    bool miningActive => this.target != null;

    void Update()
    {
        // Mine the target
        if (this.miningActive)
        {
            if (this.target.IsInRange(this.transform) && !this.target.IsComplete())
            {
                this.target.Mine(this); // It's mine!!
                if (this.target.IsComplete())
                {
                    // Mining is done, decide what to do
                    var asteroidLogic = this.target.GetComponent<AsteroidLogic>();

                    // Explode asteroid if attached to asteroid
                    if (asteroidLogic != null)
                    {
                        asteroidLogic.Explode();
                    }

                    // Give health if miner is on player
                    var healthComp = this.GetComponent<HealthComponent>();
                    if (healthComp != null)
                    {
                        healthComp.AddHull(0.35f);
                    }

                    this.target = null;
                }
            }
            else
            {
                this.target.ResetMining();
                this.target = null;
            }
        }

        // Update mining effect
        this.miningEffect.gameObject.SetActive(this.miningActive);
        if (this.miningActive)
        {
            var vectorToTarget = this.target.originTransform.position - this.transform.position;
            this.miningEffect.transform.rotation =
                Quaternion.Euler(0, 0, Vector2.SignedAngle(Vector2.right, vectorToTarget));
                // * Quaternion.Euler(0, 45f, 0);
            this.miningEffect.transform.localScale = new Vector3(vectorToTarget.magnitude, 1, 1);
        }
    }

    public void StartMining()
    {
        this.target = EffectSource.GetNearest<Mineable>(this.transform); // Might return null
    }
}
