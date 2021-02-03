// unset

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SectionedSimPath
{
    public Vector3 position;
    public Vector3 velocity;
    public Vector3 relativeVelocity;
    public bool crashed { get; private set; } = false;
    public bool willCrash => this.simPath?.willCrash ?? false;

    private readonly SimModel model;
    private readonly int targetTicks;
    private readonly int sectionTicks;
    private readonly float dt;
    private readonly float gravitationalConstant;
    private readonly float gravitationalRescaling;
    private readonly float collisionRadius;
    private readonly bool disableFuturePath;

    private SimPath simPath;
    private int simTick;
    private bool sectionIsQueued = false;
    private bool restartPath = true;

    public SectionedSimPath(SimModel model, int startSimTick, Vector3 startPosition, Vector3 startVelocity, int targetTicks, float dt, float gravitationalConstant, float gravitationalRescaling, float collisionRadius, bool disableFuturePath = false, int sectionTicks = 200)
    {
        this.model = model;
        this.targetTicks = targetTicks;
        this.sectionTicks = sectionTicks;
        this.dt = dt;
        this.gravitationalConstant = gravitationalConstant;
        this.gravitationalRescaling = gravitationalRescaling;
        this.collisionRadius = collisionRadius;
        this.disableFuturePath = disableFuturePath;

        this.position = startPosition;
        this.velocity = startVelocity;
        this.simTick = startSimTick;
    }

    private static readonly List<Vector3> Empty = new List<Vector3>();

    public List<Vector3> GetAbsolutePath() => this.simPath?.pathSection.positions ?? Empty;

    public PathSection GetRelativePath(GravitySource g) => this.simPath?.relativePaths[g];

    public IEnumerable<SimModel.SphereOfInfluence> GetFullPathSOIs() => this.simPath?.sois ?? new List<SimModel.SphereOfInfluence>();

    public bool Step(int tick, Vector3 force, int timeStep)
    {
        this.simTick = tick;

        this.simPath?.TrimStart(this.simTick);

        // Get new position, either from the path or via dead reckoning
        if (this.simPath != null && this.simPath.HaveCrashed(this.simTick))
        {
            this.position = this.simPath.crashPosition;
            this.crashed = true;
        }
        else if (this.simPath != null && this.simPath.pathSection.InRange(this.simTick) && force.magnitude == 0)
        {
            (this.position, this.velocity) = this.simPath.pathSection.GetPositionVelocityHermite(this.simTick, this.dt);

            if (this.simPath.sois.Any())
            {
                var firstSoi = this.simPath.sois.First();
                var orbitComponent = firstSoi.g.GetComponent<Orbit>();
                this.relativeVelocity = orbitComponent != null ?
                    this.velocity - orbitComponent.absoluteVelocity
                    :
                    this.velocity;
            }
            else
            {
                this.relativeVelocity = this.velocity;
            }
        }
        else
        {
            // Hopefully we will never hit this for more than a frame
            var forceInfo = this.model.CalculateForce(this.simTick * this.dt, this.position, this.gravitationalConstant, this.gravitationalRescaling);
            float timeStepDt = this.dt * timeStep;
            if (forceInfo.valid)
            {
                this.velocity += forceInfo.rescaledTotalForce * timeStepDt;
            }
            this.velocity += force * timeStepDt;
            var prevPosition = this.position;
            this.position += this.velocity * timeStepDt;
            
            
            // Detect if we will crash between the last step and this one
            var collision = this.model.DetectCrash(forceInfo, prevPosition, this.position - prevPosition, this.collisionRadius);
            if (collision.occurred)
            {
                this.position = collision.at;
                this.crashed = true;
            }
            else
            {
                this.crashed = false;
            }
            
            this.relativeVelocity = forceInfo.valid 
                ? this.velocity - forceInfo.velocities[forceInfo.primaryIndex] 
                : this.velocity;
            
            // Start recreating the path sections
            this.restartPath = true;
        }
        
        Debug.DrawLine(this.position, this.position + this.velocity, Color.red);

        if (!this.disableFuturePath &&
            !this.sectionIsQueued && 
            (this.restartPath ||
             this.simPath == null ||
             this.simPath.pathSection.durationTicks < this.targetTicks && !this.willCrash))
        {
            this.GenerateNewSection();
        }

        return !this.crashed;
    }

    // float GetTotalPathDuration() => this.path?..Select(p => p.duration).Sum();

    private async void GenerateNewSection()
    {
        this.sectionIsQueued = true;

        if (this.restartPath || this.simPath == null)
        {
            this.restartPath = false; // Reset this to allow it to be set again immediately if required
            this.simPath = await this.model.CalculateSimPathAsync(this.position, this.velocity, this.simTick, this.dt, this.targetTicks, this.collisionRadius, this.gravitationalConstant, this.gravitationalRescaling);
        }
        else
        {
            this.simPath.Append(await this.model.CalculateSimPathAsync(this.simPath.pathSection.finalPosition, this.simPath.pathSection.finalVelocity, this.simPath.pathSection.endTick, this.dt, this.sectionTicks, this.collisionRadius, this.gravitationalConstant, this.gravitationalRescaling));
        }

        this.sectionIsQueued = false;
    }
}