using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    // List of enemies we can collide with, we initiate it at Start()
    private List<ControllerBase> enemiesCached;

    // Start is called before the first frame update
    new protected void Start()
    {
        base.Start();

        //Debug.Log($"Frame {Time.frameCount} RocketUnguidedController.Start() <-");

        this.movement = this.GetComponentInParent<SimMovement>();
        var engine = this.GetComponent<EngineController>();
        engine.thrust.y = this.thrust;
        this.movement.OnCrashed.AddListener(() => Object.Destroy(this.gameObject));

        //Debug.Log("RocketUnguidedController.Start() ->");

    }

    private void Awake()
    {
        this.enemiesCached = FindObjectsOfType<ControllerBase>().Where(c => c.faction != this.faction).ToList();    
    }

    // Update is called once per frame
    private void Update()
    {
        // Process movement
        var engine = this.GetComponent<EngineController>();

        this.movement.alignToVelocity = this.thrustTime <= 0.0f;
        engine.thrust.y = this.thrustTime <= 0.0f ? 0 : this.thrust;

        this.thrustTime -= Time.deltaTime;
        this.lifeTime -= Time.deltaTime;

        if (this.lifeTime < 0.0f)
        {
            Object.Destroy(this.gameObject);
        }

        var simMovement = this.GetComponent<SimMovement>();
        float thisRadius = simMovement.collisionRadius;
        Vector3 thisPos = simMovement.simPosition;

        var crashObject = this.enemiesCached.FirstOrDefault(nmy => {
            var simComp = nmy.GetComponent<SimMovement>();
            float dist = Vector3.Distance(simComp.simPosition, thisPos);
            return dist <= thisRadius + simComp.collisionRadius;
        });

        if (crashObject != null)
        {
            Debug.Log($"Rocket {this} has hit {crashObject}");
            Object.Destroy(this.gameObject);
            var healthComp = crashObject.GetComponent<HealthComponent>();
            if (healthComp != null)
            {
                healthComp.AddDamage(9000, new Vector3(1, 0, 0));
            }
        }
    }
}
