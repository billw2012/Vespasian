using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponComponentMachinegun : WeaponComponentBase
{
    [SerializeField]
    private GameObject projectilePrefab = null;

    [SerializeField]
    float startVelocity = 1.0f;

    protected override void FireInternal(Vector3 fireDir) {
        this.InstantiateProjectile(this.projectilePrefab, fireDir, this.startVelocity);
    }
}
