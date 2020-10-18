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
        // Estimate discovery radius by object's size
        var planetLogic = this.GetComponent<BodyLogic>();
        if (planetLogic != null)
        {
            this.discoveryRadius = planetLogic.radius / 2.0f * 30.0f;
        }

    }

    private void Update()
    {
        // Other components often add renderers in dynamic way,
        // Such as shadow renderer, or ring renderer, or maybe others in the future
        // Thus we update this every frame
        // todo: improve it if it affects performance

        this.renderers = this.GetComponentsInChildren<Renderer>();
        //Debug.Log($"Object: {this}, Renderers {this.renderers.Length}:");
        //foreach (var r in this.renderers)
        //Debug.Log($"     {r}");

        this.EnableAllRenderers(this.discovered);
    }

    public void Discover()
    {
        this._discovered = true;
    }

    void EnableAllRenderers(bool enable)
    {
        //Debug.Log($"EnableAllRenderers: {this}");
        foreach (var rendererComponent in this.renderers)
        {
            if (rendererComponent != null)
            {
                rendererComponent.enabled = enable;
                //Debug.Log($"Renderer {rendererComponent} enabled: {enable}");
            }
        }
    }
}
