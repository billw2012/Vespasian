using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

public class BodyGenerator : MonoBehaviour
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

    public virtual float tempK => this.body.temp;
    public float mass => this.body.mass;
    public float radius => this.body.radius;


    public void Init(Body body, float danger)
    {
        this.body = body;

        Random.InitState(body.randomKey);

        // Orbit setup
        var orbit = this.GetComponent<Orbit>();
        if (orbit != null)
        {
            orbit.parameters = body.parameters;
        }

        // Body characteristics
        var bodyLogic = this.GetComponent<BodyLogic>();
        if (bodyLogic != null)
        {
            bodyLogic.radius = body.radius;
            bodyLogic.dayPeriod = MathX.RandomGaussian(5, 30 * body.mass) * Mathf.Sign(Random.value - 0.5f);
        }

        // Gravity
        var gravitySource = this.GetComponent<GravitySource>();
        if (gravitySource)
        {
            gravitySource.autoMass = false;
            gravitySource.parameters.mass = body.mass;
            gravitySource.parameters.density = body.density;
            //float volume = 4f * Mathf.PI * Mathf.Pow(body.density, 3) / 3f;
            //gravitySource.parameters.density = body.mass / volume;
        }

        this.InitInternal(body, danger);
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if(this.body != null && this.GetComponent<GravitySource>() != null)
        {
            Handles.color = Color.yellow;
            GUIUtils.Label(this.GetComponent<GravitySource>().target.position + Vector3.down * this.radius * 1.25f, $"{this.tempK:0}°K\n{this.radius:0.00}R\n{this.mass:0.00}M");
        }
    }
#endif

    protected virtual void InitInternal(Body body, float danger) { }
}
