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

    [NonSerialized]
    public float danger;

    public void Init(Body body, float danger)
    {
        this.body = body;
        this.danger = danger;

        Random.InitState(body.randomKey);

        this.body.Apply(this.gameObject);

        this.InitInternal();
    }

    protected virtual void InitInternal() { }
}
