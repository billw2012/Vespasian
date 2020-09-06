using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(Orbit))]
public class OrbitObjective : Objective
{
    public float radius = 5f;

    void Start()
    {
        
    }

    void Update()
    {
        var player = FindObjectOfType<PlayerLogic>();
        var obj = this.GetComponent<Orbit>().position;
    }

    public override float GetScore() => 1;
    public override bool IsComplete() => false;

    public bool required;
    public override bool IsRequired() => this.required;

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Handles.color = Color.green;
        Handles.DrawWireDisc(Vector3.zero, Vector3.forward, this.radius);
    }
#endif
}
