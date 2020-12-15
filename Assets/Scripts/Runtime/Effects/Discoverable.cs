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

    private DataCatalog dataCatalog;

    private BodyRef bodyRef;
    private Renderer[] renderers;

    public bool discovered =>
        this.dataCatalog == null ||
        this.bodyRef == null ||
        this.dataCatalog.HaveData(this.bodyRef, DataMask.Orbit);

    public void Discover()
    {
        Debug.Log($"{this.bodyRef} was discovered");
        this.dataCatalog.AddData(this.bodyRef, DataMask.Orbit);
        NotificationsUI.Add($"<color=#00FFC3><b>{this.bodyRef}</b> was discovered!</color>");
    }

    private void Start()
    {
        // We can cache these, anything added later is presumed to not be part of the body itself, and thus not
        // required to show/hide for discovery.
        this.renderers = this.GetComponentsInChildren<Renderer>();
        
        // Estimate discovery radius by object's size
        var planetLogic = this.GetComponent<BodyLogic>();
        if (planetLogic != null)
        {
            this.discoveryRadius = planetLogic.radius / 2.0f * 30.0f;
        }

        this.dataCatalog = FindObjectOfType<PlayerController>()?.GetComponent<DataCatalog>();
        this.bodyRef = this.GetComponent<BodyGenerator>()?.BodyRef;
    }

    private void Update() => this.EnableAllRenderers(this.discovered);

    private void EnableAllRenderers(bool enable)
    {
        // TODO: do something cleaner and more efficient than disable/enable renderers, not sure what...
        foreach (var rendererComponent in this.renderers)
        {
            rendererComponent.enabled = enable;
        }
    }
}
