using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Background : MonoBehaviour
{
    void Update()
    {
        this.GetComponent<MeshRenderer>().material.SetTextureOffset("_BaseMap", this.transform.position * -0.02f);
    }
}
