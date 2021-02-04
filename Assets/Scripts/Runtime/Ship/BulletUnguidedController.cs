using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BulletUnguidedController : ControllerBase
{
    [SerializeField]
    private float lifeTime = 10.0f;

    private SimMovement movement;

    // List of enemies we can collide with, we initiate it at Start()
    private List<ControllerBase> enemiesCached;

    // Start is called before the first frame update
    new protected void Start()
    {
        base.Start();
        this.movement = this.GetComponentInParent<SimMovement>();
        this.movement.OnCrashed.AddListener(() => Object.Destroy(this.gameObject));
        this.movement.alignToVelocity = false;
        this.enemiesCached = FindObjectsOfType<ControllerBase>().Where(c => c.faction != this.faction).ToList();
    }

    // Update is called once per frame
    private void Update()
    {
        // Remove dead enemies
        this.enemiesCached.RemoveAll(e => e == null);
        
        this.lifeTime -= Time.deltaTime;

        if (this.lifeTime < 0.0f)
        {
            Object.Destroy(this.gameObject);
        }

        var simMovement = this.GetComponent<SimMovement>();
        Vector3 thisPos = simMovement.simPosition;

        var crashObject = this.enemiesCached.FirstOrDefault(nmy => {
            var simComp = nmy.GetComponent<SimMovement>();
            float dist = Vector3.Distance(simComp.simPosition, thisPos);
            return dist <= simComp.collisionRadius;
        });

        if (crashObject != null)
        {
            Debug.Log($"Bullet {this} has hit {crashObject}");
            Object.Destroy(this.gameObject);
            var healthComp = crashObject.GetComponent<HealthComponent>();
            if (healthComp != null)
            {
                healthComp.AddDamage(0.2f, new Vector3(1, 0, 0));
            }
        }
    }
}
