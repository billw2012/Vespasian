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

    public bool discovered =>
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
        var bodyGenerator = this.GetComponent<BodyGenerator>();
        this.bodyRef = bodyGenerator.BodyRef;
        this.bodyName = bodyGenerator.body.name ?? this.bodyRef.ToString();
    }

    private void Update() => this.EnableAllRenderers(this.discovered);
    
    public void Discover()
    {
        Debug.Log($"{bodyName} was discovered");
        this.dataCatalog.AddData(this.bodyRef, DataMask.Orbit);
        NotificationsUI.Add($"<color=#00FFC3><b>{bodyName}</b> was discovered!</color>");
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
