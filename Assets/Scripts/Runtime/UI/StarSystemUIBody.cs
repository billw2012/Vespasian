using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// This object is attached to celestial body prefabs in the scheme

public class StarSystemUIBody : MonoBehaviour
{
    [SerializeField]
    TextMeshProUGUI bodyName_tmp;

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

    // Sets the shown name above the body model
    public string bodyName
    {
        set
        {
            this.bodyName_tmp.text = value;
        }

        get
        {
            return this.bodyName_tmp.text;
        }
    }

}
