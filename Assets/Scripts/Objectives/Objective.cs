using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Objective : MonoBehaviour
{
    public abstract bool IsComplete();
    public abstract bool IsRequired();
    public abstract float GetScore();
}
