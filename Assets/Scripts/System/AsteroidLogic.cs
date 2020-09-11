using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AsteroidLogic : MonoBehaviour
{
    // Start is called before the first frame update

    // Axis around which we are rotating
    private Vector3 rotationAxis;

    public GameLogic gameLogic;

    [Tooltip("Root object to destroy when this asteroid is destroyed. For compatibility with orbiting asteroids.")]
    public GameObject rootDestroy;

    // Rotation speed
    private float rotationVelocity;
    void Start()
    {
        rotationAxis = Random.onUnitSphere;
        rotationVelocity = 50; // Random.Range(-40, 40);
    }

    // Update is called once per frame
    void Update()
    {
        GetComponent<Transform>().Rotate(rotationAxis, Time.deltaTime*rotationVelocity);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.GetComponentInParent<PlayerLogic>() != null)
        {
            //this.gameLogic.LoseGame();
            this.Explode();
        }
    }

    public void Explode()
    {
        var particleSystem = GetComponentInChildren<ParticleSystem>();
        particleSystem.Play();
        Destroy(this.rootDestroy, particleSystem.main.duration);
        var meshRenderer = GetComponentInChildren<MeshRenderer>();
        meshRenderer.enabled = false;
    }
}
