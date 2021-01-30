using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class HullBar : MonoBehaviour
{
    public HealthComponent target;

    private void Update()
    {
        this.GetComponent<Slider>().value = this.target.hull;
    }
}
