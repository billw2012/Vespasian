using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI.Extensions;

public class MapLinkMarkerUI : MonoBehaviour
{
    public MapComponent mapComponent;
    public Link link;
    public Color color = Color.white;
    public Color jumpRouteColor = Color.green;

    public bool isJumpRoute => this.link.Match(this.mapComponent.currentSystem, this.mapComponent.jumpTarget);

    UILineRenderer line;

    void Start()
    {
        this.line = this.GetComponent<UILineRenderer>();
        this.line.Points = new[] {
            this.link.from.position,
            this.link.to.position
        };
    }

    void Update()
    {
        this.line.color = this.isJumpRoute ? this.jumpRouteColor : this.color;
    }
}
