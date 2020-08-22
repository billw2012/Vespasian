using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class FuelBar : MonoBehaviour
{
    public GameObject fill;

    Slider slider;

    void Start()
    {
        Assert.IsNotNull(this.fill);
        this.slider = this.GetComponent<Slider>();
        Assert.IsNotNull(this.slider);
    }

    // Update is called once per frame
    void Update()
    {
        this.slider.value = GameLogic.Instance.remainingFuel;
        if (GameLogic.Instance.remainingFuel < 0.2)
        {
            this.fill.GetComponent<Image>().color = Color.red;
        }
        else if (GameLogic.Instance.remainingFuel > 1)
        {
            this.fill.GetComponent<Image>().color = new Color(0.2f, 1.0f, 0.2f);
        }
    }
}
