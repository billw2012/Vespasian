using UnityEngine;
using UnityEngine.Assertions;

[RequireComponent(typeof(Rigidbody))]
public class SimMovement : MonoBehaviour
{
    public GameConstants constants;

    public bool alignToVelocity = true;

    [HideInInspector]
    public Vector3 velocity = Vector3.zero;

    [HideInInspector]
    // Tracks correct simulated position, as rigid body is not perfectly matching the SimManager generated paths
    public Vector3 simPosition;

    Vector3 force = Vector3.zero;

    GravitySource[] gravitySources;

    void OnValidate()
    {
        Assert.IsNotNull(this.constants);
    }

    void Start()
    {
        this.OnValidate();

        this.gravitySources = GravitySource.All();
        this.simPosition = this.transform.position;
    }

    public void AddForce(Vector3 force)
    {
        this.force += force;
    }

    public void SimUpdate()
    {
        this.velocity += (this.force + this.GetGravityForce(this.simPosition)) * Time.fixedDeltaTime;
        this.simPosition += this.velocity * Time.fixedDeltaTime;

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

        this.force = Vector3.zero;
    }

    Vector3 GetGravityForce(Vector3 pos)
    {
        var force = Vector3.zero;
        foreach(var g in this.gravitySources)
        {
            force += GravityParameters.CalculateForce(pos, g.position, g.parameters.mass, this.constants.GravitationalConstant);
        }
        return force;
    }
}