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

    GameObject simulationPath = null;
    //CancellationTokenSource simCT = null;
    bool calculating = false;

    float rotVelocity;

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

    private static Vector3 CalculateForce(Vector3 from, Vector3 to, float toMass)
    {
        var vec = to - from;
        return GameConstants.Instance.GlobalCoefficient * GameConstants.Instance.AccelerationCoefficient * vec.normalized * toMass / Mathf.Pow(vec.magnitude, 2);
    }

    private static Vector3 GetForce(Vector3 pos) => 
        GravitySource.All
                .Select(src => CalculateForce(pos, src.transform.position, src.Mass))
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

    // Todo: perhaps add dynamic timestep for more efficient calculation / more resolution under high forces
    //private static int StepsForForce(float force) => (int)Mathf.Clamp(force, GameConstants.Instance.MinPhysicsSteps, GameConstants.Instance.MaxPhysicsSteps);

    void FixedUpdate()
    {
        var steps = GameConstants.Instance.MaxPhysicsSteps; //StepsForForce(force.magnitude); for if/when dynamic time step is added

        var rigidBody = this.GetComponent<Rigidbody>();
        var pos = rigidBody.position;
        var stepTime = Time.fixedDeltaTime / steps;
        for (int i = 0; i < steps; i++)
        {
            var force = GetForce(pos);
            if(this.canThrust)
            {
                force += this.velocity.normalized * this.thrust;
                GameLogic.Instance.AddFuel(-Mathf.Abs(this.thrust) * stepTime * GameConstants.Instance.FuelUse);
            }
            this.velocity += force * stepTime;
            pos += this.velocity * stepTime;
        }
        rigidBody.MovePosition(pos);

        // Smooth rotation slightly to avoid random flipping. Smoothing should not be noticible in
        // normal play.
        var desiredRot = Quaternion.FromToRotation(Vector3.up, this.velocity).eulerAngles.z;
        var currentRot = rigidBody.rotation.eulerAngles.z;
        var smoothedRot = Mathf.SmoothDampAngle(currentRot, desiredRot, ref this.rotVelocity, 0.01f, 90);
        rigidBody.MoveRotation(Quaternion.AngleAxis(smoothedRot, Vector3.forward));

        this.Simulate(this.velocity).ContinueWith(_ => {});
    }

    void OnGUI()
    {

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

    public async Task Simulate(int steps, float stepTime, Vector3 initialVelocity)
    {
        if (this.calculating)
        {
            return;
        }
        LineRenderer lineRenderer;
        if (this.simulationPath == null)
        {
            this.simulationPath = new GameObject();
            lineRenderer = this.simulationPath.AddComponent<LineRenderer>();
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startWidth = 0.02f;
            lineRenderer.startColor = new Color(1, 1, 1, 0.5f);
            lineRenderer.endColor = new Color(1, 1, 1, 0);
        }
        else
        {
            lineRenderer = this.simulationPath.GetComponent<LineRenderer>();
        }

        var simPos = this.transform.position;
        var simVelocity = initialVelocity;
        var path = new List<Vector3>();

        this.calculating = true;
        var srcs = GravitySource.All.Select(src => new { src.transform.position, src.Mass }).ToArray();

        await Task.Run(() =>
        {
            for (int step = 0; step < steps; step++)
            {
                path.Add(simPos);
                var force = srcs
                    .Select(src => CalculateForce(simPos, src.position, src.Mass))
                    .Aggregate((a, b) => a + b);
                if(force.magnitude > 20)
                {
                    break;
                }
                simVelocity += force * stepTime;
                simPos += simVelocity * stepTime;
            }
            path.Add(simPos);
        });
        var fullLength = 0.0f;
        for (int i = 0; i < path.Count - 1; i++)
        {
            fullLength += Vector3.Distance(path[i], path[i + 1]);
        }
        lineRenderer.positionCount = path.Count;
        lineRenderer.endWidth = fullLength / 30.0f;
        lineRenderer.SetPositions(path.ToArray());
        this.calculating = false;
    }

    public void ClearSimulation()
    {
        if(this.simulationPath != null)
        {
            Destroy(this.simulationPath);
            this.simulationPath = null;
        }
    }

    public async Task Simulate(float time, int iterationsPerSecond, Vector3 initialVelocity)
    {
        var stepTime = time / (float)iterationsPerSecond;
        //Time.fixedDeltaTime / (float)GameConstants.Instance.PhysicsSteps;
        int steps = (int)(time / stepTime);
        await Simulate(steps, stepTime, initialVelocity);
    }

    public async Task Simulate(Vector3 initialVelocity)
    {
        var stepTime = 0.1f; // Time.fixedDeltaTime / (float)GameConstants.Instance.PhysicsSteps;
        int steps = (int)(5 / stepTime); // (int)(1 / stepTime);
        await Simulate(steps, stepTime, initialVelocity);
    }
}