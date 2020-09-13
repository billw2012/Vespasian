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

    float miningProgress = 0; // Ranges 0..1
    bool wasMinedThisFrame = false; // Set by Mine() call on each frame

    bool completionDone = false; // Set to true after completion event

    Miner[] miners;

    // Start is called before the first frame update
    void Start()
    {
        this.miningRadiusRenderer.transform.localScale = this.maxRadius * 2 * new Vector3(1, 1, 1);
        this.miners = FindObjectsOfType<Miner>();
    }

    private void OnValidate()
    {
        this.miningRadiusRenderer.transform.localScale = this.maxRadius * 2 * new Vector3(1, 1, 1);
    }

    // Update is called once per frame
    void Update()
    {
        // Check if any miners are close enough. If so, enable the circle indicator
        var anyMinersNearby = this.miners.Any(obj =>
        {
            return obj != null ? this.maxRadius * 3 > Vector3.Distance(this.transform.position, obj.transform.position) : false;
        });
        this.miningRadiusRenderer.enabled = anyMinersNearby;
    }

    private void LateUpdate()
    {
        // Update mining damage effect
        if (this.miningDamageEffect != null)
        {
            this.miningDamageEffect.SetEmissionRateOverTimeMultiplier(this.wasMinedThisFrame ? 30.0f : 0);
        }

        this.wasMinedThisFrame = false;
    }

    // Must be called in update!!
    // If something is mining this, then it must call this method each frame
    public void Mine(Miner miner)
    {
        this.miningProgress += (1.0f / 3.0f) * Time.deltaTime;
        this.wasMinedThisFrame = true;
        //Debug.Log($"Mining progress: {this.miningProgress}");
        if (this.miningProgress >= 1.0f && !this.completionDone)
        {
            // Extra code can be put here which fill run
            // on this component once when mining is done

            this.completionDone = true;
        }
    }

    // Returns true if it still makes sense to mine this
    public override bool IsEmpty()
    {
        return miningProgress >= 1.0f;
    }

    public void ResetMining()
    {
        this.miningProgress = 0;
    }
}
