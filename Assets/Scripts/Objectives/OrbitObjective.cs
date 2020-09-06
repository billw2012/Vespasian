using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class OrbitObjective : Objective
{
    [Range(0, 10f)]
    public float orbitMaxRadius = 5f;
    [Range(0.25f, 10f)]
    public float requiredOrbits = 1;
    public bool makeRequired;
    public GameObject objectiveMarker;

    bool isOrbit;
    Vector2 lastRelativePosition;
    float performedOrbits = 0;

    static Vector2 ScreenToGUI(Vector2 screen)
    {
        return new Vector2(screen.x, Screen.height - screen.y);
    }

    void Start()
    {
        var marker = Instantiate(this.objectiveMarker, this.target);
        marker.transform.localScale = Vector3.one * this.radius;
    }

    void OnGUI()
    {
        var screenPos = Camera.main.WorldToScreenPoint(this.target.position);
        //var bounds = this.gameObject.GetFullMeshRendererBounds();
        var screenSize = Camera.main.WorldToScreenPoint(this.target.position + Vector3.one * radius) -
            Camera.main.WorldToScreenPoint(this.target.position);

        GUI.Box(new Rect(ScreenToGUI(screenPos + screenSize), Vector2.one * 100), $"orbit {this.performedOrbits:0.00} / {this.requiredOrbits}", this.style);
    }

    // Use FixedUpdate as we are tracking position of objects that are updated in FixedUpdate
    void FixedUpdate()
    {
        var player = FindObjectOfType<PlayerLogic>();
        if(player == null)
        {
            return;
        }
        var gravity = this.GetComponentInParent<GravitySource>();

        var newRelativePosition = (Vector2)(player.transform.position - this.target.position);
        var velocity = (newRelativePosition - this.lastRelativePosition) / Time.deltaTime;

        float angleDt = Vector2.Angle(this.lastRelativePosition, newRelativePosition);

        this.lastRelativePosition = newRelativePosition;

        // doing: record relative position, use
        this.isOrbit = 
            // In range
            newRelativePosition.magnitude < this.radius &&
            // In an elliptical orbit (must check this before checking semi major axis or we might get divide by zero)
            OrbitalUtils.OrbitDescriminator(velocity.magnitude, newRelativePosition.magnitude, gravity.parameters.mass, gravity.constants.GravitationalConstant) > 0 &&
            // Orbital major axis is okay
            OrbitalUtils.SemiMajorAxis(velocity.magnitude, newRelativePosition.magnitude, gravity.parameters.mass, gravity.constants.GravitationalConstant) < this.radius;

        if(this.isOrbit)
        {
            this.performedOrbits += angleDt / 360f;
        }
    }

    #region Objective implementation
    public override float amountRequired => this.requiredOrbits;
    public override float amountDone => this.performedOrbits;
    public override bool required => this.makeRequired;
    public override float score => this.performedOrbits / this.requiredOrbits;
    public override bool active => this.isOrbit;
    public override Transform target => this.GetComponentInParent<Orbit>().position;
    public override float radius => this.orbitMaxRadius;
    #endregion

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Handles.color = Color.green;
        Handles.DrawWireDisc(this.target.position, Vector3.forward, this.radius);
    }
#endif
}
