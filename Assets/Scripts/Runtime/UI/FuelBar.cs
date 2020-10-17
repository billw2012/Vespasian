using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class FuelBar : MonoBehaviour
{
    public PlayerController player;

    void Update()
    {
        var engine = this.player.GetComponentInChildren<EngineComponent>();
        foreach (Transform child in this.transform)
        {
            child.gameObject.SetActive(engine != null);
        }

        if (engine != null)
        {
            this.GetComponent<Slider>().value = engine.fuel;
        }
    }
}
