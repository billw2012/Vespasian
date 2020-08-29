using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AsteroidLogic : MonoBehaviour
{
    // Start is called before the first frame update

    // Axis around which we are rotating
    private Vector3 rotationAxis;

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
}
