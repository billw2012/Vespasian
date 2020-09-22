using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Mineable : EffectSource
{
    [Tooltip("Damage particle effect, used for mining")]
    public ParticleSystem miningDamageEffect;

    [Tooltip("Renders sprite with radius where we can mine this from")]
    public SpriteRenderer miningRadiusRenderer;

    [NonSerialized]
    public float miningProgress = 0; // Ranges 0..1

    [NonSerialized]
    public bool beingMined = false; // Set by Mine() call on each frame

    public bool destroyed => !this.asteroidLogic.enabled;

    AsteroidLogic asteroidLogic;
    Miner[] miners;

    // Start is called before the first frame update
    void Start()
    {
        this.miningRadiusRenderer.transform.localScale = this.range * 2 * Vector3.one;
        this.miners = FindObjectsOfType<Miner>();
        this.asteroidLogic = this.GetComponent<AsteroidLogic>();
    }

    void OnValidate()
    {
        this.miningRadiusRenderer.transform.localScale = this.range * 2 * Vector3.one;
    }

    // Update is called once per frame
    void Update()
    {
        // Check if any miners are close enough. If so, enable the circle indicator
        bool AnyMinersNearby() => this.miners.Any(obj => obj != null && this.range * 3 >= this.GetDistance(obj.transform));

        this.miningRadiusRenderer.enabled = !this.IsEmpty() && AnyMinersNearby();
    }

    void LateUpdate()
    {
        // Update mining damage effect
        if (this.miningDamageEffect != null)
        {
            if(this.beingMined && !this.miningDamageEffect.isPlaying)
            {
                this.miningDamageEffect.Play();
            }
            else if(!this.beingMined && this.miningDamageEffect.isPlaying)
            {
                this.miningDamageEffect.Stop();
            }
        }

        this.asteroidLogic.temperature = Mathf.Clamp01(
            this.asteroidLogic.temperature + (this.beingMined ? 1 : -1) * Time.deltaTime * 0.2f
            );
        this.beingMined = false;
    }

    // Must be called in update!!
    // If something is mining this, then it must call this method each frame
    public void Mine(Miner miner)
    {
        this.miningProgress = Mathf.Clamp01(this.miningProgress + miner.miningRate * Time.deltaTime);

        this.beingMined = true;
        this.miningDamageEffect.transform.rotation = Quaternion.FromToRotation(Vector3.left, miner.transform.position - this.transform.position);
    }

    // Returns true if it still makes sense to mine this
    public override bool IsEmpty()
    {
        return this.miningProgress >= 1f || this.destroyed;
    }

    public void ResetMining()
    {
        this.miningProgress = 0;
    }

    public override Color gizmoColor => Color.gray;
    public override string debugName => "Mineable";
}
