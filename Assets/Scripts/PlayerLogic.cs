using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class PlayerLogic : MonoBehaviour
{
    [HideInInspector]
    public Vector3 velocity = Vector3.zero;
    [HideInInspector]
    public float thrust { get; set; } = 0;
    bool canThrust => this.velocity.magnitude != 0 && GameLogic.Instance.remainingFuel > 0;

    public ParticleSystem frontThruster;
    public ParticleSystem rearThruster;
    public ParticleSystem damageDebris;

    static void SetEmissionActive(ParticleSystem pfx, bool enabled)
    {
        var em = pfx.emission;
        if(em.enabled != enabled)
        {
            em.enabled = enabled;
        }
    }

    void Start()
    {
        Debug.Assert(this.frontThruster != null);
        Debug.Assert(this.rearThruster != null);
        Debug.Assert(this.damageDebris != null);

        SetEmissionActive(this.rearThruster, false);
        SetEmissionActive(this.frontThruster, false);
        SetEmissionActive(this.damageDebris, false);
    }

    private static Vector3 GetForce(Vector3 pos) => 
        GravitySource.All
                .Select(src => GravitySource.CalculateForce(pos, src.transform.position, src.mass))
                .Aggregate((a, b) => a + b);

    void Update()
    {
        var rearThrusterModule = this.rearThruster.emission;
        rearThrusterModule.enabled = this.canThrust && this.thrust > 0;
        rearThrusterModule.rateOverTimeMultiplier = GameLogic.Instance.remainingFuel * 100;
        var frontThrusterModule = this.frontThruster.emission;
        frontThrusterModule.enabled = this.canThrust && this.thrust < 0;
        frontThrusterModule.rateOverTimeMultiplier = GameLogic.Instance.remainingFuel * 100;
    }

    void FixedUpdate()
    {
        var rigidBody = this.GetComponent<Rigidbody>();
        var force = GetForce(rigidBody.position);
        if(this.canThrust)
        {
            force += this.velocity.normalized * this.thrust;
            GameLogic.Instance.AddFuel(-Mathf.Abs(this.thrust) * Time.fixedDeltaTime * GameConstants.Instance.FuelUse);
        }
        this.velocity += force * Time.fixedDeltaTime;
        var pos = rigidBody.position + this.velocity * Time.fixedDeltaTime;
        rigidBody.MovePosition(pos);

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

    public void SetTakingDamage(float damageRate, Vector3 direction)
    {
        SetEmissionActive(this.damageDebris, damageRate > 0);
        if (damageRate > 0)
        {
            this.damageDebris.transform.rotation = Quaternion.FromToRotation(Vector3.up, direction);
            var emission = this.damageDebris.emission;
            emission.rateOverTimeMultiplier = damageRate * 100;
        }
    }
}