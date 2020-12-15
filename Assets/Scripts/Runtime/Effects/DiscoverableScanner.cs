using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

class DiscoverableScanner : MonoBehaviour
{
    private DataCatalog dataCatalog;

    private void Awake()
    {
        this.dataCatalog = this.GetComponent<DataCatalog>();
    }

    private void Update()
    {
        // TODO: optimize, need to detect when system changes...
        // Clean up any already discovered
        foreach (var obj in Object.FindObjectsOfType<Discoverable>()
            .Where(u => 
                !u.discovered 
                && Vector3.Distance(
                    u.originTransform.position,
                    this.transform.position
                    ) < u.discoveryRadius
                ))
        {
            obj.Discover();
        }
    }
}
