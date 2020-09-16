using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class FuelBar : MonoBehaviour
{
    public GameObject fill;
    public PlayerLogic target;

    void Update()
    {
        this.GetComponent<Slider>().value = this.target.fuelCurrent;
    }
}
