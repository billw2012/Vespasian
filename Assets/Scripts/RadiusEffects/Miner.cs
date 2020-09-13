using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Miner : MonoBehaviour
{
    public ParticleSystem miningEffect;

    Mineable target = null;

    bool miningActive => target != null;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // Mine the target
        if (this.miningActive)
        {
            if (this.target.IsInEffectRange(this.transform) && !target.IsEmpty())
            {
                this.target.Mine(this); // It's mine!!
                if (this.target.IsEmpty())
                {
                    // Mining is done, decide what to do
                    var asteroidLogic = this.target.GetComponent<AsteroidLogic>();

                    // Explode astoroid if attached to asteroid
                    if (asteroidLogic != null)
                        asteroidLogic.Explode();

                    // Give health if miner is on player
                    var playerLogic = GetComponent<PlayerLogic>();
                    if (playerLogic != null)
                        playerLogic.AddHealth(0.35f);

                    this.StopMining();
                    this.target = null;
                }
            }
            else
            {
                this.StopMining();
            }
        }

        // Update mining effect
        this.miningEffect.gameObject.SetActive(this.miningActive);
        if (this.miningActive)
        {
            var vectorToTarget = this.target.effectSourceTransform.position - this.transform.position;
            this.miningEffect.transform.rotation =
                Quaternion.Euler(0, 0, Vector2.SignedAngle(Vector2.right, vectorToTarget));// *
                                                                                           //Quaternion.Euler(0, 45f, 0);
            this.miningEffect.transform.localScale = new Vector3(vectorToTarget.magnitude, 1, 1);
        }
    }

    public void StartMining()
    {
        // Search for nearby mining targets
        var closestMineable = EffectSource.GetNearestEffectSource<Mineable>(this.transform); // Might return null
        this.target = closestMineable;
    }

    public void StopMining()
    {
        if (this.target != null)
            this.target.ResetMining();
        this.target = null;
    }
}
