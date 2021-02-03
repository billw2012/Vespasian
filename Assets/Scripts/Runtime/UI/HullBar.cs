using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class HullBar : MonoBehaviour
{
    private HealthComponent target;

    private void Start() => this.target = FindObjectOfType<PlayerController>().GetComponent<HealthComponent>();

    private void Update() => this.GetComponent<Slider>().value = this.target.hull;
}
