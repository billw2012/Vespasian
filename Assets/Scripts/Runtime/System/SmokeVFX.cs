using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmokeVFX : MonoBehaviour
{
    // Transform of the planets parent (what it is orbiting)
    public Transform orbitOrigin;
    // Transform of the planet itself, including scale
    public Transform planet;

    private void Update()
    {
        var direction = this.planet.position - this.orbitOrigin.position;
        this.transform.rotation = Quaternion.FromToRotation(Vector2.right, direction);
        this.transform.localScale = this.planet.localScale;
    }
}
