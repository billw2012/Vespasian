using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// This object is attached to celestial body prefabs in the scheme

public class StarSystemUIBody : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI bodyName_tmp = default;

    public Image selectorImage;             // Link to selector image

    public StarSystemUI starSystemUI;       // Link back to star system UI

    public OrbitingBody actualBody;     // Link to the body from which this is generated

    /*
    // Start is called before the first frame update
    void Start()
    {
        
    }
    */

    /*
    // Update is called once per frame
    void Update()
    {
        
    }
    */

    // Sets/gets the shown name above the body model
    public string bodyName
    {
        set => this.bodyName_tmp.text = value;

        get => this.bodyName_tmp.text;
    }

    public void OnClick() => this.starSystemUI.OnSchemeBodyClick(this);
}
