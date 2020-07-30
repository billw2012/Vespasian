using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class GravityAffected : MonoBehaviour
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

    private void FixedUpdate()
    {
        // Todo: extract the steps function
        // Todo: make optimized version that determines number of steps from force magnitude (don't need as many when force is low)
        var stepTime = Time.fixedDeltaTime / (float)GameConstants.Instance.PhysicsSteps;
        for (int i = 0; i < GameConstants.Instance.PhysicsSteps; i++)
        {
            var force = GravitySource.All
                .Select(src => CalculateForce(this.transform.position, src.transform.position, src.Mass))
                .Aggregate((a, b) => a + b);
            this.velocity += force * stepTime;
            this.transform.position += this.velocity * stepTime;
        }

        this.transform.rotation = Quaternion.FromToRotation(Vector3.up, this.velocity);
    }

    public async Task Simulate(int steps, float stepTime, Vector3 initialVelocity)
    {
        if(this.calculating)
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
            lineRenderer.startWidth = lineRenderer.endWidth = 0.02f;
        }
        else
        {
            lineRenderer = this.simulationPath.GetComponent<LineRenderer>();
        }

        lineRenderer.positionCount = steps + 1;

        var simPos = this.transform.position;
        var simVelocity = initialVelocity;
        var path = new Vector3[steps + 1];

        // Cancel previous task if there was one
        //this.simCT?.Cancel();
        //this.simCT?.Dispose();
        //this.simCT = new CancellationTokenSource();
        //try
        //{
            this.calculating = true;
            //var ct = this.simCT.Token;
            var srcs = GravitySource.All.Select(src => new { src.transform.position, src.Mass }).ToArray();
            await Task.Run(() =>
            {
                for (int step = 0; step < steps; step++)
                {
                    //ct.ThrowIfCancellationRequested();
                    path[step] = simPos;
                    var force = srcs
                        .Select(src => CalculateForce(simPos, src.position, src.Mass))
                        .Aggregate((a, b) => a + b);
                    simVelocity += force * stepTime;
                    simPos += simVelocity * stepTime;
                }
                path[steps] = simPos;
            }/*, ct*/);
            lineRenderer.SetPositions(path);
            this.calculating = false;
        //}
        //catch (TaskCanceledException)
        //{

        //}
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
        int steps = (int)(20 / stepTime); // (int)(1 / stepTime);
        await Simulate(steps, stepTime, initialVelocity);
    }
}