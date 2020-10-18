using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

class DiscoverableScanner : MonoBehaviour
{
    private void Update()
    {
        var discoverables = Object.FindObjectsOfType<Discoverable>();
        foreach (var obj in discoverables)
        {
            if (!obj.discovered)
            {
                float dist = Vector3.Distance(obj.originTransform.position, this.transform.position);
                if (dist < obj.discoveryRadius)
                {
                    obj.Discover();
                }
            }
        }
    }
}
