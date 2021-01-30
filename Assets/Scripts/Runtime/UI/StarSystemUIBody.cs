using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// This object is attached to celestial body prefabs in the scheme

public class StarSystemUIBody : MonoBehaviour
{
    [SerializeField]
    private TMP_Text bodyNameLabel = default;

    /// <summary>
    /// Link to selector image
    /// </summary>
    public Image selectorImage;
    
    /// <summary>
    /// Icon that can be toggled to indicate a station orbiting this body
    /// </summary>
    public Image stationIcon;

    /// <summary>
    /// Root transform under which the bodies specific icon will be instanced
    /// </summary>
    public RectTransform iconRoot;
    
    /// <summary>
    /// Link back to star system UI
    /// </summary>
    [NonSerialized]
    public StarSystemUI starSystemUI;

    /// <summary>
    /// Link to the body from which this is generated
    /// </summary>
    public Body actualBody;
 
    /// <summary>
    /// The name shown above the body model
    /// </summary>
    public string bodyName
    {
        set => this.bodyNameLabel.text = value;
        get => this.bodyNameLabel.text;
    }

    public void OnClick() => this.starSystemUI.SelectBody(this);
}
