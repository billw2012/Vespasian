using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class FuelBar : MonoBehaviour
{
    public GameObject fill;
    public EngineComponent engine;

    void Update()
    {
        this.GetComponent<Slider>().value = this.engine.fuelCurrent;
    }
}
