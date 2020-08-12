using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Background : MonoBehaviour
{
    void Update()
    {
        // Do this every time, as screen size can change, and its a very cheap calculation
        var bl = Camera.main.ScreenToWorldPoint(Vector3.zero);
        var tr = Camera.main.ScreenToWorldPoint(Screen.width * Vector3.right + Screen.height * Vector3.up);
        var worldSize = tr - bl;
        var size = this.GetComponent<MeshFilter>().mesh.bounds.size.x;
        this.transform.localScale = Vector3.one * Mathf.Max(worldSize.x, worldSize.y) / size;
        this.GetComponent<MeshRenderer>().material.SetTextureOffset("_BaseMap", this.transform.position * -0.02f);
    }
}
