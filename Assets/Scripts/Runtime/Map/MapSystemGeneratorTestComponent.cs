using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapSystemGeneratorTestComponent : MonoBehaviour
{
    public BodySpecs bodySpecs;

    void Start()
    {
        this.Regenerate();
    }

    public void Regenerate()
    {
        var system = MapGenerator.GenerateSystem((int)(DateTime.Now.Ticks % int.MaxValue), this.bodySpecs, "test", Vector2.zero);
        system.Load(this.gameObject);
    }
}
