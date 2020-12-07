using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Attach it to something which can be discovered by player.
class Discoverable : MonoBehaviour, ISavable
{
    public float discoveryRadius = 10.0f;

    [Tooltip("Transform to use as the effect source")]
    public Transform originTransform = default;

    [Saved]
    public bool discovered = false;

    private void Start()
    {
        // Estimate discovery radius by object's size
        var planetLogic = this.GetComponent<BodyLogic>();
        if (planetLogic != null)
        {
            this.discoveryRadius = planetLogic.radius / 2.0f * 30.0f;
        }
    }

    private void Update()
    {
        this.EnableAllRenderers(this.discovered);
    }

    private void EnableAllRenderers(bool enable)
    {
        // Renderers can't be cached as they might be added or removed during game play
        // TODO: do something cleaner and more efficient than disable/enable renderers, not sure what...
        foreach (var rendererComponent in this.GetComponentsInChildren<Renderer>())
        {
            rendererComponent.enabled = enable;
        }
    }
}
