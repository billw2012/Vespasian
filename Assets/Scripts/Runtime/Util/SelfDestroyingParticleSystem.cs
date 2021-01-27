using System;
using System.Linq;
using UnityEngine;

public class SelfDestroyingParticleSystem : MonoBehaviour
{
    [SerializeField]
    private bool auto = true;
    [SerializeField]
    private ParticleSystem[] pfx;
    [SerializeField]
    private GameObject destroyRoot;

    private void Awake()
    {
        if (this.auto)
        {
            this.pfx = this.GetComponentsInChildren<ParticleSystem>();
        }

        if (this.destroyRoot == null)
        {
            this.destroyRoot = this.gameObject;
        }
    }

    private void Update()
    {
        if (this.pfx.All(p => !p.IsAlive()))
        {
            Destroy(this.gameObject);
        }
    }
}
