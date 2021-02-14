using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RadarActiveTargetIndicatorComponent : MonoBehaviour
{
    // We just use these for storage
    public Vector3 velocity;
    public Vector3 worldPosition;
    public Image[] images;
    public float timer;

    public RectTransform pointerTransform;  // We need this to rotate the pointer according to velocity
}
