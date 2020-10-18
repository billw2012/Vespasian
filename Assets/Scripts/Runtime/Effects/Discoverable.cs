using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Attach it to something which can be discovered by player.

class Discoverable : MonoBehaviour
{
    private bool _discovered = false;

    private Renderer[] renderers;

    public float discoveryRadius = 10.0f;

    [Tooltip("Transform to use as the effect source")]
    public Transform originTransform;

    public bool discovered
    {
        get { return this._discovered; }
    }

    private void Start()
    {
        this.renderers = this.GetComponentsInChildren<Renderer>();
        Debug.Log($"Object: {this}, Renderers {this.renderers.Length}:");
        foreach (var r in this.renderers)
            Debug.Log($"     {r}");

        this.EnableAllRenderers(false);

        // Estimate discovery radius by object's size
        var planetLogic = this.GetComponent<BodyLogic>();
        if (planetLogic != null)
        {
            this.discoveryRadius = planetLogic.radius / 2.0f * 30.0f;
        }

    }

    public void Discover()
    {
        this._discovered = true;
        this.EnableAllRenderers(true);
    }

    void EnableAllRenderers(bool enable)
    {
        foreach (var rendererComponent in this.renderers)
        {
            rendererComponent.enabled = enable;
            Debug.Log($"Renderer {rendererComponent} enabled: {enable}");
        }
    }
}
