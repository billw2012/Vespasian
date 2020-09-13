﻿using System.Collections;
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

    // Mining
    bool exploded = false;
    public bool HasExploded { get { return this.exploded; } }

    // Axis around which we are rotating
    private Vector3 rotationAxis;
    private float rotationVelocity;

    // Player object
    PlayerLogic playerLogic;
    
    void Start()
    {
        rotationAxis = Random.onUnitSphere;
        rotationVelocity = 50; // Random.Range(-40, 40);
        this.playerLogic = this.gameLogic.GetPlayerLogic();
    }

    // Update is called once per frame
    void Update()
    {
        asteroidModelTransform.Rotate(rotationAxis, Time.deltaTime*rotationVelocity); 
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
}
