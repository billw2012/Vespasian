using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RocketUnguidedController : ControllerBase
{
    [SerializeField]
    private float thrustTime = 2.0f;
    [SerializeField]
    private float lifeTime = 10.0f;
    [SerializeField]
    private float thrust = 3.0f;

    private SimMovement movement;

    // Start is called before the first frame update
    private void Start()
    {
        this.movement = this.GetComponentInParent<SimMovement>();
        var engine = this.GetComponent<EngineController>();
        engine.thrust.y = this.thrust;
        this.movement.OnCrashed.AddListener(() => Object.Destroy(this.gameObject));
    }

    // Update is called once per frame
    private void Update()
    {
        var engine = this.GetComponent<EngineController>();

        this.movement.alignToVelocity = this.thrustTime <= 0.0f;
        engine.thrust.y = this.thrustTime <= 0.0f ? 0 : this.thrust;

        this.thrustTime -= Time.deltaTime;
        this.lifeTime -= Time.deltaTime;

        if (this.lifeTime < 0.0f)
        {
            Object.Destroy(this.gameObject);
        }
    }
}
