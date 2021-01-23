﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RocketUnguidedController : ControllerBase
{
    private SimMovement movement;

    public float thrustTime = 2.0f;
    public float lifeTime = 10.0f;

    public float thrust = 3.0f;

    // Start is called before the first frame update
    void Start()
    {
        this.movement = this.GetComponentInParent<SimMovement>();
        var engine = this.GetComponent<EngineController>();
        engine.thrust.y = this.thrust;
    }

    // Update is called once per frame
    void Update()
    {
        var engine = this.GetComponent<EngineController>();
        engine.thrust.y = this.thrustTime <= 0.0f ? 0 : this.thrust;

        this.thrustTime -= Time.deltaTime;
        this.lifeTime -= Time.deltaTime;

        if (this.lifeTime < 0.0f)
        {
            Object.Destroy(this.gameObject);
        }
    }
}
