﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Miner : MonoBehaviour
{
    public ParticleSystem miningEffect;

    Mineable target = null;

    bool miningActive => target != null;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // Mine the target
        if (this.miningActive)
        {
            var distanceToTarget = Vector3.Distance(this.transform.position, this.target.transform.position);
            if (distanceToTarget < this.target.miningRadius && target.CanBeMined)
            {
                this.target.Mine(this); // It's mine!!
            }
            else
            {
                this.StopMining();
            }
        }

        // Update mining effect
        this.miningEffect.gameObject.SetActive(this.miningActive);
        if (this.miningActive)
        {
            var vectorToTarget = this.target.transform.position - this.transform.position;
            this.miningEffect.transform.rotation =
                Quaternion.Euler(0, 0, Vector2.SignedAngle(Vector2.right, vectorToTarget));// *
                                                                                           //Quaternion.Euler(0, 45f, 0);
            this.miningEffect.transform.localScale = new Vector3(vectorToTarget.magnitude, 1, 1);
        }
    }

    public void StartMining()
    {
        // Search for nearby mining targets
        var mineables = Object.FindObjectsOfType<Mineable>();
        var mineablesSorted = mineables.OrderBy(i => Vector3.Distance(i.transform.position, this.transform.position)).ToArray();
        if (mineablesSorted.Length > 0)
        {
            var closest = mineablesSorted[0];
            var closestDistance = Vector3.Distance(closest.transform.position, this.transform.position);
            if (closestDistance < closest.miningRadius)
            {
                this.target = closest;
            }
        }
    }

    public void StopMining()
    {
        if (this.target != null)
            this.target.ResetMining();
        this.target = null;
    }
}
