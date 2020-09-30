using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class ShieldBar : MonoBehaviour
{
    public HealthComponent target;

    void Update()
    {
        this.GetComponent<Slider>().value = this.target.shield;
    }
}
