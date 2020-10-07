using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * This is the passive docking port, to be attached to space stations
 */

public class DockPassive : EffectSource
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public override Color gizmoColor => Color.red;
    public override string debugName => "DockPassive";
}
