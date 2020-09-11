using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Background : MonoBehaviour
{
    public float speedMultiplier = 0.1f;

    // LateUpdate used as we need the ensure the background is always fitted to the final camera size and position
    Vector2 lastPosition;
    Vector2 offset;

    float parallaxSpeed => this.speedMultiplier / Camera.main.orthographicSize;

    void Start()
    {
        this.lastPosition = this.transform.position;
        this.offset = this.transform.position * -this.parallaxSpeed;
    }

    void LateUpdate()
    {
        // Do this every time, as screen size can change, and its a very cheap calculation
        var bl = Camera.main.ScreenToWorldPoint(Vector3.zero);
        var tr = Camera.main.ScreenToWorldPoint(Screen.width * Vector3.right + Screen.height * Vector3.up);
        var worldSize = tr - bl;
        float size = this.GetComponent<MeshFilter>().mesh.bounds.size.x;
        this.transform.localScale = Vector3.one * Mathf.Max(worldSize.x, worldSize.y) / size;

        var movement = (Vector2)this.transform.position - this.lastPosition;
        this.lastPosition = this.transform.position;
        this.offset += movement * -this.parallaxSpeed;
        this.GetComponent<MeshRenderer>().material.SetTextureOffset("_MainTex", this.offset);
    }
}
