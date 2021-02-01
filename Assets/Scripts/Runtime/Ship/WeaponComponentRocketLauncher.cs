using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponComponentRocketLauncher : WeaponComponentBase
{
    [SerializeField]
    private GameObject projectilePrefab = null;

    protected override void FireInternal(Vector3 fireDir) {
        this.InstantiateProjectile(this.projectilePrefab, fireDir, 0);
    }
}
