using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponComponentMachinegun : WeaponComponentBase
{
    [SerializeField]
    private GameObject projectilePrefab = null;

    protected override void BeforeLateUpdate()
    {
        
    }

    protected override void FireInternal(Vector3 fireDir) {
        this.InstantiateProjectile(this.projectilePrefab, fireDir, this.projectileStartVelocity);
    }
}
