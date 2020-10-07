﻿using System;
using UnityEngine;

[Serializable]
public class WeightedRandom
{
    public AnimationCurve weighting = AnimationCurve.Linear(0, 0, 1, 1);
    public bool gaussian = false;
    public float min;
    public float max;
    public float Evaluate(float randomValue) => this.gaussian ? MathX.RandomGaussian(this.min, this.max) : Mathf.Lerp(this.min, this.max, this.weighting.Evaluate(randomValue));
}