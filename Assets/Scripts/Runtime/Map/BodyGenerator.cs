using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class BodyGenerator : MonoBehaviour
{
    [NonSerialized]
    public Body body;

    [NonSerialized]
    public float danger;

    public void Init(Body body, RandomX rng, float danger)
    {
        this.body = body;
        this.danger = danger;

        this.InitInternal(rng);
    }

    protected virtual void InitInternal(RandomX rng) { }
}
