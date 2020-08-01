using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class PlayerLogic : MonoBehaviour
{
    public Vector3 velocity = Vector3.zero;

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

        var pos = this.GetComponent<Rigidbody>().position;
        var stepTime = Time.fixedDeltaTime / steps;
        for (int i = 0; i < steps; i++)
        {
            var force = GetForce(pos);
            this.velocity += force * stepTime;
            pos += this.velocity * stepTime;
        }
        this.GetComponent<Rigidbody>().MovePosition(pos);
        this.GetComponent<Rigidbody>().MoveRotation(Quaternion.FromToRotation(Vector3.up, this.velocity));
        // this.transform.rotation = ;
    }

    public async Task Simulate(int steps, float stepTime, Vector3 initialVelocity)
    {
        if (this.calculating)
        {
            return;
        }
        //steps = 100;
        //stepTime = 0.5f;
        Debug.Log($"steps = {steps} stepTime = {stepTime}");
        LineRenderer lineRenderer;
        if (this.simulationPath == null)
        {
            this.simulationPath = new GameObject();
            lineRenderer = this.simulationPath.AddComponent<LineRenderer>();
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            //lineRenderer.material = new Material(Shader.Find("Particles/Additive"));
            lineRenderer.startWidth = 0.02f;
            lineRenderer.endWidth = 1.0f;
            lineRenderer.startColor = Color.white;
            lineRenderer.endColor = new Color(0, 0, 0, 0);
        }
        else
        {
            lineRenderer = this.simulationPath.GetComponent<LineRenderer>();
        }

        lineRenderer.positionCount = steps + 1;

        var simPos = this.transform.position;
        var simVelocity = initialVelocity;
        var path = new Vector3[steps + 1];

        this.calculating = true;
        var srcs = GravitySource.All.Select(src => new { src.transform.position, src.Mass }).ToArray();
        await Task.Run(() =>
        {
            for (int step = 0; step < steps; step++)
            {
                path[step] = simPos;
                var force = srcs
                    .Select(src => CalculateForce(simPos, src.position, src.Mass))
                    .Aggregate((a, b) => a + b);
                simVelocity += force * stepTime;
                simPos += simVelocity * stepTime;
            }
            path[steps] = simPos;
        });
        lineRenderer.SetPositions(path);
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
        var stepTime = 0.01f; // Time.fixedDeltaTime / (float)GameConstants.Instance.PhysicsSteps;
        int steps = (int)(5 / stepTime); // (int)(1 / stepTime);
        await Simulate(steps, stepTime, initialVelocity);
    }
}