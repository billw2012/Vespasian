using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public abstract class BodyGenerator : MonoBehaviour
{
    //[Tooltip("Radius min"), Range(0, 20)]
    //public float radiusMin = 1;
    //[Tooltip("Radius range"), Range(0, 30)]
    //public float radiusRange = 10;

    //[Tooltip("Density min"), Range(0, 1)]
    //public float densityMin = 0.1f;
    //[Tooltip("Density range"), Range(0, 5)]
    //public float densityRange = 1.4f;

    [NonSerialized]
    public Body body;

    public void Init(Body body, float danger)
    {
        this.body = body;

        Random.InitState(body.randomKey);

        // Orbit setup
        this.GetComponent<Orbit>().parameters = body.parameters;

        // Body characteristics
        var bodyLogic = this.GetComponent<BodyLogic>();
        bodyLogic.radius = body.radius;
        bodyLogic.dayPeriod = MathX.RandomGaussian(1, 10 * body.mass) * Mathf.Sign(Random.value - 0.5f);

        // Gravity
        var gravitySource = this.GetComponent<GravitySource>();
        gravitySource.autoMass = false;
        gravitySource.parameters.mass = body.mass;
        float volume = 4f * Mathf.PI * Mathf.Pow(body.radius, 3) / 3f;
        gravitySource.parameters.density = body.mass / volume;

        this.InitInternal(body, danger);
    }

    protected abstract void InitInternal(Body body, float danger);
}
