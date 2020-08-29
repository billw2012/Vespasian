using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class ScanBar : MonoBehaviour
{
    public ScanEffect target;

    Image fill;
    Slider slider;

    void Start()
    {
        Assert.IsNotNull(this.target);

        this.fill = this.transform.Find("Fill Area/Fill").GetComponent<Image>();
        Assert.IsNotNull(this.fill);
        this.slider = this.GetComponent<Slider>();
        Assert.IsNotNull(this.slider);
    }

    void Update()
    {
        this.slider.value = this.target.scanned;
        if (this.target.scanned == 1)
        {
            this.fill.color = Color.green;
        }
    }
}
