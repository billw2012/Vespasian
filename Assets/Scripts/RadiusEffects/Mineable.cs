﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Mineable : MonoBehaviour
{
    [Tooltip("Damage particle effect, used for mining")]
    public ParticleSystem miningDamageEffect;

    public float miningRadius = 3.0f;
    public SpriteRenderer miningRadiusRenderer;
    float miningProgress = 0; // Ranges 0..1
    float updatesWithoutMining = 0;

    Miner[] miners;

    // Start is called before the first frame update
    void Start()
    {
        this.miningRadiusRenderer.transform.localScale = this.miningRadius * 2 * new Vector3(1, 1, 1);
        this.miners = FindObjectsOfType<Miner>();
    }

    private void OnValidate()
    {
        this.miningRadiusRenderer.transform.localScale = this.miningRadius * 2 * new Vector3(1, 1, 1);
    }

    // Update is called once per frame
    void Update()
    {
        // Check if any miners are close enough. If so, enable the circle indicator
        var anyMinersNearby = this.miners.Any(obj =>
        {
            return obj != null ? this.miningRadius * 3 > Vector3.Distance(this.transform.position, obj.transform.position) : false;
        });
        this.miningRadiusRenderer.enabled = anyMinersNearby;

        this.updatesWithoutMining ++;

        if (this.miningDamageEffect != null)
        {
            this.miningDamageEffect.SetEmissionRateOverTimeMultiplier(this.updatesWithoutMining > 1 ? 0 : 30.0f);
        }
    }

    // If something is mining this, then it must call this method each frame
    public void Mine(Miner miner)
    {
        this.miningProgress += (1.0f / 3.0f) * Time.deltaTime;
        this.updatesWithoutMining = 0;
        //Debug.Log($"Mining progress: {this.miningProgress}");
        if (this.miningProgress >= 1.0f)
        {
            // Mining is done, decide what to do
            var asteroidLogic = GetComponent<AsteroidLogic>();

            // Explode astoroid if attached to asteroid
            if (asteroidLogic != null)
                asteroidLogic.Explode();

            // Give health if miner is on player
            var playerLogic = miner.GetComponent<PlayerLogic>();
            if (playerLogic != null)
               playerLogic.AddHealth(0.2f);
        }
    }

    // Returns true if it still makes sense to mine this
    public bool CanBeMined
    {
        get
        {
            // If attached to asteroid, it makes sense to mine it until it has exploded
            var asteroidLogic = GetComponent<AsteroidLogic>();
            if (asteroidLogic != null)
            {
                return !asteroidLogic.HasExploded;
            }

            // Else it's always mineable
            return true;
        }
    }

    public void ResetMining()
    {
        this.miningProgress = 0;
    }
}
