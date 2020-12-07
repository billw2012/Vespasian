using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class BodyGenerator : MonoBehaviour
{
    [NonSerialized]
    public Body body;
    
    [NonSerialized]
    public SolarSystem system;

    public void Init(Body body, RandomX rng, SolarSystem system)
    {
        this.body = body;
        this.system = system;

        this.InitInternal(rng);
    }

    protected virtual void InitInternal(RandomX rng) { }
}
