﻿using UnityEngine;

/// <summary>
/// Core asteroid state and behaviour
/// </summary>
public class AsteroidLogic : MonoBehaviour
{
    // Objects to set in editor
    public GameLogic gameLogic;

    [Tooltip("Root object to deactivate when this asteroid is destroyed")]
    public GameObject model;

    [Tooltip("Must set it to asteroid's transform so we can rotate it")]
    public Transform asteroidModelTransform;

    public ParticleSystem[] explodeParticleSystems;

    [HideInInspector]
    public float temperature = 0;

    public Color coldColor = new Color(0.37f, 0.37f, 0.37f);
    public Color hotColor = new Color(1, 0.13f, 0);

    // Axis around which we are rotating
    private Vector3 rotationAxis;
    private float rotationVelocity;
    private float rotationOffset;

    private Simulation simManager;

    private MaterialPropertyBlock asteroidPb;
    private MeshRenderer meshRenderer;

    private void Awake()
    {
        this.simManager = ComponentCache.FindObjectOfType<Simulation>();
        this.asteroidPb = new MaterialPropertyBlock();
    }

    private void Start()
    {
        this.rotationAxis = Random.onUnitSphere;
        this.rotationVelocity = 50; // Random.Range(-40, 40);
        this.meshRenderer = this.asteroidModelTransform.GetComponentInChildren<MeshRenderer>();
        this.rotationOffset = Random.Range(0, 360);
        //this.asteroidModelTransform.Rotate(this.rotationAxis, Random.Range(0, 360));
    }

    // Update is called once per frame
    private void FixedUpdate()
    {
        float time = this.simManager == null ? Time.time : this.simManager.time;
        this.asteroidModelTransform.localRotation = Quaternion.AngleAxis(this.rotationOffset + time * this.rotationVelocity, this.rotationAxis);
    }

    private void Update()
    {
        if(this.meshRenderer.HasPropertyBlock())
        {
            this.meshRenderer.GetPropertyBlock(this.asteroidPb);
        }
        this.asteroidPb.SetColor("_EmissionColor", Color.Lerp(this.coldColor, this.hotColor, this.temperature));
        this.meshRenderer.SetPropertyBlock(this.asteroidPb);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.GetComponentInParent<PlayerController>() != null)
        {
            this.Explode();

            var shakeEffect = ComponentCache.FindObjectOfType<CameraShakeEffect>();
            if(shakeEffect != null)
            {
                shakeEffect.StartShake(1f);
            }
        }
    }

    // Makes this asteroid explode, the model and collision and this script are deactivated
    public void Explode()
    {
        foreach(var pfx in this.explodeParticleSystems)
        {
            pfx.Play();
        }
        this.model.SetActive(false);
        this.enabled = false;
    }
}
