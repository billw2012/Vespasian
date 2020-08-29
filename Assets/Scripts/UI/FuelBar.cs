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

    Slider slider;

    void Start()
    {
        Assert.IsNotNull(this.target);
        Assert.IsNotNull(this.fill);
        this.slider = this.GetComponent<Slider>();
        Assert.IsNotNull(this.slider);
    }

    // Update is called once per frame
    void Update()
    {
        float fuelRatio = this.target.fuelCurrent;
        this.slider.value = fuelRatio;
        if (fuelRatio < 0.2)
        {
            this.fill.GetComponent<Image>().color = Color.red;
        }
        else if (fuelRatio >= 1)
        {
            this.fill.GetComponent<Image>().color = new Color(0.2f, 1.0f, 0.2f);
        }
    }
}
