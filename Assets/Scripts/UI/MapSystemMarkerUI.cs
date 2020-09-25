using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MapSystemMarkerUI : MonoBehaviour
{
    public MapComponent mapComponent;
    public SolarSystem system;
    public Color color = Color.gray;
    public Color currentColor = Color.white;
    public Color jumpTargetColor = Color.green;
    public GameObject currentMarker;

    public TextMeshProUGUI label;

    public bool current => this.system == this.mapComponent.currentSystem;
    public bool jumpTarget => this.system == this.mapComponent.GetJumpTarget();

    Image image;

    void Start()
    {
        var instTransform = this.transform as RectTransform;
        instTransform.anchorMin = this.system.position;
        instTransform.anchorMax = this.system.position;

        this.image = this.GetComponent<Image>();

        this.label.text = this.system.name;

        this.label.enabled = false;
        this.currentMarker.SetActive(false);
    }

    void Update()
    {
        this.image.color = this.current
            ? this.currentColor
            : this.jumpTarget
            ? this.jumpTargetColor
            : this.color;

        this.currentMarker.SetActive(this.current);
    }
}
