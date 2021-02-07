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
        // Clean up any already discovered
        foreach (var obj in ComponentCache.FindObjectsOfType<Discoverable>()
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
