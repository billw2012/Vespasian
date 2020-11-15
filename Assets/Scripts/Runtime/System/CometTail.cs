﻿using Pixelplacement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CometTail : MonoBehaviour
{
    public float minDistance = 10f;
    public float maxDistance = 300f;
    public AnimationCurve falloff = Tween.EaseOut;

    Transform star;
    ParticleSystem[] pfx;

    // Start is called before the first frame update
    void Start()
    {
        this.star = this.GetComponentInParentOnly<StarLogic>().geometryTransform;
        this.pfx = this.GetComponentsInChildren<ParticleSystem>();

        foreach(var p in this.pfx)
        {
            p.Play();
        }
    }

    // Update is called once per frame
    void Update()
    {
        var starVec = this.transform.position - this.star.position;
        this.transform.rotation = Quaternion.FromToRotation(Vector3.up, starVec);

        //float distFactor = 1 - this.falloff.Evaluate(Mathf.Clamp01(Mathf.InverseLerp(this.minDistance, this.maxDistance, starVec.magnitude)));

        //foreach (var p in this.pfx)
        //{
        //    p.SetMainValues(m => m.startSpeedMultiplier = distFactor);
        //    p.SetEmissionRateOverTimeMultiplier(distFactor);
        //}
    }
}