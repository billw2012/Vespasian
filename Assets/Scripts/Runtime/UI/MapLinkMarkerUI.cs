using UnityEngine;
using UnityEngine.UI.Extensions;

public class MapLinkMarkerUI : MonoBehaviour
{
    public MapComponent mapComponent;
    public Link link;
    public Color color = Color.white;
    public Color jumpRouteColor = Color.green;
    
    private SolarSystem from => this.mapComponent.map.GetSystem(this.link.from);
    private SolarSystem to => this.mapComponent.map.GetSystem(this.link.to);

    private bool isJumpRoute => this.mapComponent.currentSystem != null 
                                && this.mapComponent.jumpTarget != null 
                                && this.link.Match(this.mapComponent.currentSystem.id, this.mapComponent.jumpTarget.id);

    private UILineRenderer line;

    private void Start()
    {
        this.line = this.GetComponent<UILineRenderer>();
        this.line.Points = new[] {
            this.from.position,
            this.to.position
        };
    }

    private void Update()
    {
        this.line.color = this.isJumpRoute ? this.jumpRouteColor : this.color;
    }

    public void Refresh()
    {
        
    }
}
