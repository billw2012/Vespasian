using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class HullBar : MonoBehaviour
{
    public HealthComponent target;

    void Update()
    {
        this.GetComponent<Slider>().value = this.target.hull;
    }
}
