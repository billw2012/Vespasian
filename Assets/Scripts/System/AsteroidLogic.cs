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

    // Axis around which we are rotating
    private Vector3 rotationAxis;
    private float rotationVelocity;
    
    void Start()
    {
        this.rotationAxis = Random.onUnitSphere;
        this.rotationVelocity = 50; // Random.Range(-40, 40);
    }

    // Update is called once per frame
    void Update()
    {
        this.asteroidModelTransform.Rotate(this.rotationAxis, Time.deltaTime* this.rotationVelocity); 
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.GetComponentInParent<PlayerController>() != null)
        {
            this.Explode();
            FindObjectOfType<FollowCameraController>().StartShake(1f);
        }
    }

    // Makes this asteroid explode, it is deleted after the explosion
    public void Explode()
    {
        var particleSystem = this.GetComponentInChildren<ParticleSystem>();
        particleSystem.Play();
        Destroy(this.rootDestroy, particleSystem.main.duration);
        var meshRenderer = this.GetComponentInChildren<MeshRenderer>();
        meshRenderer.enabled = false;
    }
}
