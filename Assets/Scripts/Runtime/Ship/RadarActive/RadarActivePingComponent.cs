using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * This component manages a radar ping signal expanding in space and revealing objects.
 */

public class RadarActivePingComponent : MonoBehaviour
{
    private Simulation simulation;
    private RadarActiveComponent radar;             // Radar which has sent this ping

    private List<RadarActiveReflector> potentialTargets;    // Objects which will potentially be detected

    private const float expansionSpeed = 110.0f;           // The 'speed of light' in our game 
    private float currentRadius = 0;
    private float maxRadius;

    // Transform of the sprite visualizing the wave front
    [SerializeField]
    private Transform vfxTransform = null;


    private void Awake()
    {
        this.simulation = ComponentCache.FindObjectOfType<Simulation>();
    }

    // Start is called before the first frame update
    private void Start()
    {

    }

    // Update is called once per frame
    private void Update()
    {
        float deltaTimeReal = Time.deltaTime * this.simulation.tickStep;

        // Handle wave front expantion
        this.currentRadius += deltaTimeReal * expansionSpeed;
        this.UpdateVisualEffects();
        if (this.currentRadius >= this.maxRadius)
        {
            Destroy(this.gameObject);
            return;
        }

        // Discover new objects
        Vector3 thisPos = this.transform.position;
        var targetsDetectedThisUpdate = new List<RadarActiveReflector>();
        foreach (var reflector in this.potentialTargets)
        {
            if (reflector != null)
            {
                if (Vector3.Distance(reflector.transform.position, thisPos) < this.currentRadius)
                {
                    // todo use LOS to check if the radar can see that target
                    this.radar.OnPingReceived(reflector);
                    targetsDetectedThisUpdate.Add(reflector);
                }
            }
        }
        foreach (var reflector in targetsDetectedThisUpdate)
        {
            this.potentialTargets.Remove(reflector);
        }
    }

    private void UpdateVisualEffects()
    {
        float scale = 2.0f*this.currentRadius;
        this.vfxTransform.transform.localScale = new Vector3(scale, scale, scale);
    }

    // Call this after instantiating the prefab
    public void InitPing(RadarActiveComponent radar, float maxRadius, Vector3 startPos)
    {
        this.radar = radar;
        this.maxRadius = maxRadius;
        this.transform.position = startPos;
        this.currentRadius = 0;
        this.potentialTargets = new List<RadarActiveReflector>(ComponentCache.FindObjectsOfType<RadarActiveReflector>());
        this.UpdateVisualEffects();
    }
}
