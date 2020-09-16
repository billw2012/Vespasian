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
        this.miningRadiusRenderer.transform.localScale = this.range * 2 * Vector3.one;
        this.miners = FindObjectsOfType<Miner>();
    }

    private void OnValidate()
    {
        this.miningRadiusRenderer.transform.localScale = this.range * 2 * Vector3.one;
    }

    // Update is called once per frame
    void Update()
    {
        // Check if any miners are close enough. If so, enable the circle indicator
        bool anyMinersNearby = this.miners.Any(obj => obj != null && this.range * 3 > this.GetDistance(obj.transform));
        this.miningRadiusRenderer.enabled = anyMinersNearby;
    }

    private void LateUpdate()
    {
        // Update mining damage effect
        if (this.miningDamageEffect != null)
        {
            this.miningDamageEffect.SetEmissionRateOverTimeMultiplier(this.wasMinedThisFrame ? 30f : 0);
        }

        this.wasMinedThisFrame = false;
    }

    // Must be called in update!!
    // If something is mining this, then it must call this method each frame
    public void Mine(Miner _)
    {
        this.miningProgress += (1f / 3f) * Time.deltaTime;
        this.wasMinedThisFrame = true;
        //Debug.Log($"Mining progress: {this.miningProgress}");
        if (this.miningProgress >= 1f && !this.completionDone)
        {
            // Extra code can be put here which fill run
            // on this component once when mining is done

            this.completionDone = true;
        }
    }

    // Returns true if it still makes sense to mine this
    public override bool IsEmpty()
    {
        return miningProgress >= 1f;
    }

    public void ResetMining()
    {
        this.miningProgress = 0;
    }

    public override Color gizmoColor => Color.gray;
    public override string debugName => "Mineable";
}
