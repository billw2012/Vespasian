using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponComponentRocketLauncher : WeaponComponentBase
{
    [SerializeField]
    private GameObject projectilePrefab = null;

    [SerializeField, Tooltip("The engine work time of the launched rocket is fixed. The thrust is calculated according to projectileStartVelocity")]
    private float thrustTime = 1.0f;

    protected override void BeforeLateUpdate()
    {
        
    }

    protected override void FireInternal(Vector3 fireDir) {
        var rocket = this.InstantiateProjectile(this.projectilePrefab, fireDir, 0);
        var rocketController = rocket.GetComponent<RocketUnguidedController>();

        // The engine work time of the launched rocket is fixed.
        // The thrust is calculated according to projectileStartVelocity
        rocketController.thrustTime = this.thrustTime;
        rocketController.thrust = this.projectileStartVelocity / this.thrustTime;
    }
}
