using UnityEngine;

// Attach it to something which can be discovered by player.
class Discoverable : MonoBehaviour
{
    public float discoveryRadius = 10.0f;

    [Tooltip("Transform to use as the effect source")]
    public Transform originTransform = default;

    private DataCatalog dataCatalog;

    private BodyRef bodyRef;
    private Renderer[] renderers;
    private BodyLogic bodyLogic;
    private string bodyName;
    private BodyGenerator bodyGenerator;

    public bool isDiscovered() =>
        this.dataCatalog == null ||
        this.bodyRef == null ||
        this.dataCatalog.HaveData(this.bodyRef, DataMask.Orbit);

    private void Start()
    {
        // We can cache these, anything added later is presumed to not be part of the body itself, and thus not
        // required to show/hide for discovery.
        this.renderers = this.GetComponentsInChildren<Renderer>();
        
        // Estimate discovery radius by object's size
        this.bodyLogic = this.GetComponent<BodyLogic>();
        if (this.bodyLogic != null)
        {
            this.discoveryRadius = this.bodyLogic.radius / 2.0f * 30.0f;
        }

        this.dataCatalog = FindObjectOfType<PlayerController>()?.GetComponent<DataCatalog>();
        this.bodyGenerator = this.GetComponent<BodyGenerator>();
        this.bodyRef = this.bodyGenerator.BodyRef;
        this.bodyName = this.bodyGenerator.body?.name ?? this.bodyRef?.ToString() ?? "(unnamed)";
    }

    private void Update()
    {
        bool discovered = this.isDiscovered();

        this.EnableAllRenderers(discovered);

        if (discovered 
            && this.bodyGenerator.body != null 
            && this.bodyGenerator.body.ApplyUniqueName()
            )
        {
            Debug.Log($"{this.bodyName} has become known as {this.bodyGenerator.body.name}");
            NotificationsUI.Add($"<color=#00DDC3><b>{this.bodyName}</b> has become known as <b>{this.bodyGenerator.body.uniqueName}</b>!</color>");

            this.bodyName = this.bodyGenerator.body.name;
        }
    }

    public void Discover()
    {
        Debug.Log($"{this.bodyName} was discovered");
        this.dataCatalog.AddData(this.bodyRef, DataMask.Orbit);
        NotificationsUI.Add($"<color=#00FFC3><b>{this.bodyName}</b> was discovered!</color>");
    }
    
    private void EnableAllRenderers(bool enable)
    {
        // TODO: do something cleaner and more efficient than disable/enable renderers, not sure what...
        foreach (var rendererComponent in this.renderers)
        {
            rendererComponent.enabled = enable;
        }
    }
}
