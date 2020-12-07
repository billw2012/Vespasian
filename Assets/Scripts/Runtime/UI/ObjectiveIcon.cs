﻿using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class ObjectiveIcon : MonoBehaviour
{
    public Objective target;

    private Image fill;
    private Slider slider;

    private void Start()
    {
        this.fill = this.transform.Find("Fill Area/Fill").GetComponent<Image>();
        this.slider = this.GetComponent<Slider>();
    }

    private void Update()
    {
        if (this.target.complete)
        {
            this.slider.value = 1;
            this.fill.color = Color.green;
        }
        else if (this.target.failed)
        {
            this.slider.value = 1;
            this.fill.color = Color.red;
        }
        else
        {
            this.slider.value = this.target.amountDone / this.target.amountRequired;
        }
    }
}
