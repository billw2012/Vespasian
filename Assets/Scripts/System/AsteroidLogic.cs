using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AsteroidLogic : MonoBehaviour
{
    // Objects to set in editor

    [Tooltip("Game logic")]
    public GameLogic gameLogic;

    [Tooltip("Root object to destroy when this asteroid is destroyed. For compatibility with orbiting asteroids.")]
    public GameObject rootDestroy;

    [Tooltip("Must set it to asteroid's transform so we can rotate it.")]
    public Transform asteroidModelTransform;

    [Tooltip("Damage particle effect, used for mining")]
    public ParticleSystem miningDamageEffect;


    // Mining
    
    public float miningRadius = 3.0f;
    public SpriteRenderer miningRadiusRenderer;
    float miningProgress = 0; // Ranges 0..1
    float timePassedWithoutMining = 0;
    bool exploded = false;
    public bool HasExploded { get { return this.exploded; } }

    // Axis around which we are rotating
    private Vector3 rotationAxis;
    private float rotationVelocity;

    // Player object
    PlayerLogic playerLogic;
    
    void Start()
    {
        this.miningRadiusRenderer.transform.localScale = this.miningRadius * 2 * (new Vector3(1,1,1));
        rotationAxis = Random.onUnitSphere;
        rotationVelocity = 50; // Random.Range(-40, 40);
        this.playerLogic = this.gameLogic.GetPlayerLogic();
    }

    private void OnValidate()
    {
        this.miningRadiusRenderer.transform.localScale = this.miningRadius * 2 * (new Vector3(1, 1, 1));
    }

    // Update is called once per frame
    void Update()
    {
        asteroidModelTransform.Rotate(rotationAxis, Time.deltaTime*rotationVelocity);

        // Check if player is close enough. If so, enable the circle indicator
        this.miningRadiusRenderer.enabled = this.miningRadius * 3 > Vector3.Distance(this.transform.position, this.playerLogic.transform.position);

        this.timePassedWithoutMining += Time.deltaTime;

        this.miningDamageEffect.SetEmissionRateOverTimeMultiplier(this.timePassedWithoutMining > 0.1f ? 0 : 30.0f);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.GetComponentInParent<PlayerLogic>() != null)
        {
            //this.gameLogic.LoseGame();
            this.Explode();
            this.gameLogic.GetCameraController().StartShake(1f);
        }
    }

    // Makes this asteroid explode, it is deleted after the explosion
    public void Explode()
    {
        var particleSystem = GetComponentInChildren<ParticleSystem>();
        particleSystem.Play();
        Destroy(this.rootDestroy, particleSystem.main.duration);
        var meshRenderer = GetComponentInChildren<MeshRenderer>();
        meshRenderer.enabled = false;
        this.exploded = true;
    }

    // If player is mining this, then it must call this method each frame
    public void Mine()
    {
        this.miningProgress += (1.0f/3.0f) * Time.deltaTime;
        this.timePassedWithoutMining = 0;
        //Debug.Log($"Mining progress: {this.miningProgress}");
        if (this.miningProgress >= 1.0f && !this.exploded)
        {
            this.Explode();
            if (this.playerLogic != null)
                this.playerLogic?.AddHealth(0.2f);
        }
    }

    public void ResetMining()
    {
        this.miningProgress = 0;
    }
}
