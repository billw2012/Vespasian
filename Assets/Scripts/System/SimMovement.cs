using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

[RequireComponent(typeof(Rigidbody))]
public class SimMovement : MonoBehaviour
{
    public GameConstants constants;

    public bool alignToVelocity = true;

    public Vector3 startVelocity;

    public Vector3 simPosition => this.path.position;
    public Vector3 velocity => this.path.velocity;
    //GravitySource[] gravitySources;

    SectionedSimPath path;
    Vector3 force = Vector3.zero;

    public LineRenderer pathRenderer;

    void OnValidate()
    {
        Assert.IsNotNull(this.constants);
    }

    void Start()
    {
        this.OnValidate();

        var simManager = FindObjectOfType<SimManager>();
        Assert.IsNotNull(simManager);

        this.path = simManager.CreateSectionedSimPath(this.transform.position, this.startVelocity, 100);

        //this.gravitySources = GravitySource.All();
        //this.simPosition = this.transform.position;
    }

    public void AddForce(Vector3 force)
    {
        this.force += force;
    }

    public void SimUpdate(float simTime, float dt)
    {
        //var position = this.path.GetPosition(simTime);

        //if (position != null)
        //{
        //    this.velocity = position.Value - this.simPosition;
        //    this.simPosition = position.Value;
        //}
        //else
        //{
        //    this.simPosition += this.velocity * dt;
        //}

        this.path.Step(simTime, this.force);

        if(this.pathRenderer != null)
        {
            var path = this.path.GetFullPath().ToArray();
            this.pathRenderer.positionCount = path.Length;
            this.pathRenderer.SetPositions(path);
        }

        //this.velocity += this.force * dt;
        this.force = Vector3.zero;

        //Vector3 direction;
        //this.velocity += (this.force + this.GetGravityForce(this.simPosition)) * dt;
        //this.simPosition += this.velocity * dt;

        var rigidBody = this.GetComponent<Rigidbody>();
        this.GetComponent<Rigidbody>().MovePosition(this.simPosition);

        if (this.alignToVelocity)
        {
            // TODO: rotation needs to be smoothed, but this commented out method results in rotation
            // while following the current orbit lagging.
            // (perhaps even limiting them to the same magnitude?)
            // Smooth rotation slightly to avoid random flipping. Smoothing should not be noticeable in
            // normal play.
            //var desiredRot = Quaternion.FromToRotation(Vector3.up, this.velocity).eulerAngles.z;
            //var currentRot = rigidBody.rotation.eulerAngles.z;
            //var smoothedRot = Mathf.SmoothDampAngle(currentRot, desiredRot, ref this.rotVelocity, 0.01f, 90);
            //rigidBody.MoveRotation(Quaternion.AngleAxis(smoothedRot, Vector3.forward));

            rigidBody.MoveRotation(Quaternion.FromToRotation(Vector3.up, this.velocity));
        }
    }

    //Vector3 GetGravityForce(Vector3 pos)
    //{
    //    var forces = new List<Vector3>(this.gravitySources.Length);

    //    float maxForce = 0;
    //    int primary = 0;
    //    for (int i = 0; i < this.gravitySources.Length; i++)
    //    {
    //        var force = OrbitalUtils.CalculateForce(g.position - pos, g.parameters.mass, this.constants.GravitationalConstant);
    //        forces.Add(force);
    //        float forceMag = force.magnitude;
    //        if (forceMag > maxForce)
    //        {
    //            maxForce = forceMag;
    //            primary = i;
    //        }
    //    }

    //    var forceTotal = Vector3.zero;
    //    for (int i = 0; i < forces.Count; i++)
    //    {
    //        this.gravitySources[primary].par
    //    }
    //    foreach (var force in forces)
    //    {
    //        forceTotal += force; //.normalized * maxForce * Mathf.Pow(force.magnitude / maxForce, this.constants.GravitationalRescaling);
    //    }
    //    return forceTotal;
    //}
}