using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class PlayerLogic : MonoBehaviour
{
    public Vector3 velocity = Vector3.zero;
    public float thrust { get; set; } = 0;

    GameObject simulationPath = null;
    //CancellationTokenSource simCT = null;
    bool calculating = false;

    private static Vector3 CalculateForce(Vector3 from, Vector3 to, float toMass)
    {
        var vec = to - from;
        return GameConstants.Instance.GlobalCoefficient * GameConstants.Instance.AccelerationCoefficient * vec.normalized * toMass / Mathf.Pow(vec.magnitude, 2);
    }

    private static Vector3 GetForce(Vector3 pos) => 
        GravitySource.All
                .Select(src => CalculateForce(pos, src.transform.position, src.Mass))
                .Aggregate((a, b) => a + b);

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
            if(this.velocity.magnitude != 0 && GameLogic.Instance.remainingFuel > 0)
            {
                force += this.velocity.normalized * thrust;
                GameLogic.Instance.AddFuel(-Mathf.Abs(thrust) * stepTime * GameConstants.Instance.FuelUse);
            }
            this.velocity += force * stepTime;
            pos += this.velocity * stepTime;
        }
        rigidBody.MovePosition(pos);
        rigidBody.MoveRotation(Quaternion.FromToRotation(Vector3.up, this.velocity));

        this.Simulate(this.velocity).ContinueWith(_ => {});
    }

    void OnGUI()
    {

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