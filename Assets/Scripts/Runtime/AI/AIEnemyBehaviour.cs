// unset

using AI.Behave;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AIEnemyBehaviour : MonoBehaviour, ISimUpdate
{
    [SerializeField]
    private float shipFollowDistance = 3f;

    private SimMovement simMovement;
    private AIController aiController;
    private Simulation simulation;
    private AI.Behave.Tree tree;

    /// <summary>
    /// Sets target velocity to current velocity
    /// </summary>
    private class Idle : AI.Behave.Node
    {
        public override Result Update(object blackboard)
        {
            var ai = (AIEnemyBehaviour)blackboard;
            ai.aiController.DisableTargetVelocity(); 
            return Result.Success;
        }
    }

    /// <summary>
    /// Wraps any node to only call its Update periodically instead of every update.
    /// Between Updates this node returns the last value.
    /// </summary>
    private class PeriodicUpdate : AI.Behave.Decorator
    {
        private readonly int intervalSimTicks;
        private Result state = Result.Failure;
        private int nextUpdateTick = 0;

        public PeriodicUpdate(int intervalSimTicks, AI.Behave.Node child) : base(child)
        {
            this.intervalSimTicks = intervalSimTicks;
        }

        public override Result Update(object blackboard)
        {
            var ai = (AIEnemyBehaviour)blackboard;
            if (ai.simulation.simTick > this.nextUpdateTick)
            {
                this.nextUpdateTick = ai.simulation.simTick + this.intervalSimTicks;
                this.state = this.child.Update(blackboard);
            }
            return this.state;
        }
    }

    private class InvertResult : AI.Behave.Decorator
    {
        public InvertResult(Node child) : base(child) {}

        public override Result Update(object blackboard)
        {
            var result = this.child.Update(blackboard);
            if (result == Result.Failure)
            {
                return Result.Success;
            }
            else if (result == Result.Success)
            {
                return Result.Failure;
            }
            return result;
        }
    }
    
    private abstract class OrbitManeuver : AI.Behave.Node
    {
        private readonly (BodyLogic b, Orbit o, GravitySource g)[] bodies;

        protected OrbitManeuver()
        {
            this.bodies = FindObjectsOfType<BodyLogic>()
                .Select(b => (b, o: b.GetComponent<Orbit>(), g: b.GetComponent<GravitySource>()))
                .ToArray();
        }

        protected abstract (Result result, Vector2? newVelocity) UpdateManeuver(Vector2 aiPos, Vector2 aiVel);
        
        /// <summary>
        /// Gets the relative orbit of the provided <paramref name="pos"/> and <paramref name="vel"/>
        /// around each of the bodies in the current system.
        /// This gets the orbits in order of distance, which is *not* the same as encounter order.
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="vel"></param>
        /// <returns></returns>
        protected IEnumerable<(BodyLogic b, AnalyticOrbit orbit)> GetOrbits(Vector2 pos, Vector2 vel) =>
            this.bodies
                .OrderBy(bog => Vector2.Distance(pos, (Vector2)bog.o.position.position))
                .Select(bog =>
                {
                    // Orbit relative to this body
                    var relVelocity = vel - (Vector2)bog.o.absoluteVelocity;
                    var relPosition = pos - (Vector2)bog.o.position.position;

                    return (bog.b, orbit: AnalyticOrbit.FromCartesianStateVector(
                        relPosition, relVelocity,
                        bog.g.parameters.mass, bog.g.constants.GravitationalConstant));
                });

        public override Result Update(object blackboard)
        {
            var ai = (AIEnemyBehaviour)blackboard;
            
            var playerPos = (Vector2)ai.transform.position;
            var playerVel = (Vector2)ai.simMovement.velocity;

            var (result, newVelocity) = this.UpdateManeuver(playerPos, playerVel);
            if(newVelocity.HasValue)
            {
                ai.aiController.SetTargetVelocity(playerVel + newVelocity.Value * 20);
            }

            return result;
        }

        protected static Vector2 ApisThrustVector(Vector2 velocity, Vector3 apsisVec)
        {
            var tVec = Vector2.Perpendicular(apsisVec.normalized);
            var forward = velocity.normalized;
            var sideways = Vector2.Perpendicular(forward);
            // Exclude any backwards thrust as it makes no sense when trying to raise an apsis (player velocity is always foward)
            var forwardComponent = forward * Mathf.Max(0, Vector2.Dot(forward, tVec));
            var sideComponent = sideways * Mathf.Max(0, Vector2.Dot(sideways, tVec));
            var clampedVec = forwardComponent + sideComponent;
            return clampedVec;
        }
    }

    /// <summary>
    /// Raises the min orbit distance to the specified value
    /// </summary>
    private class RaiseOrbit : OrbitManeuver
    {
        private readonly float timeLookAhead;
        private readonly float minHeight;
        
        public RaiseOrbit(float minHeight, float timeLookAhead = float.MaxValue) : base()
        {
            this.minHeight = minHeight;
            this.timeLookAhead = timeLookAhead;
        }

        protected override (Result result, Vector2? newVelocity) UpdateManeuver(Vector2 aiPos, Vector2 aiVel)
        {
            // Debug.DrawLine(aiPos, aiPos + aiVel * this.timeLookAhead, Color.red, duration: 1f);

            // Find the closest body where we are in a descending orbit and the orbit is too low
            var (b, orbit) = this.GetOrbits(aiPos, aiVel)
                .FirstOrDefault(bo =>
                    bo.orbit.nextapsis < this.minHeight + bo.b.radius &&
                    bo.orbit.timeOfNextapsis > 0 && bo.orbit.timeOfNextapsis < this.timeLookAhead + bo.b.radius / aiVel.magnitude);

            if (b != null)
            {
                orbit.DebugDraw(b.geometry.position, Color.red, duration: 1f);

                var nextapsisVec = orbit.isUnstable
                    ? (Vector3)aiVel
                    : -orbit.GetNextapsisPosition() * orbit.directionSign;

                var clampedVec = ApisThrustVector(aiVel, nextapsisVec);

                Debug.DrawLine(aiPos, aiPos + clampedVec * 20, Color.blue, duration: 1f);
                return (Result.Running, clampedVec);
            }
            else
            {
                return (Result.Failure, null);
            }
        }
    }

    /// <summary>
    /// If on an escape trajectory, ellipticize it
    /// </summary>
    private class StayInSystem : OrbitManeuver
    {
        private float maxRadius;
        private GravitySource star;
        
        public StayInSystem()
        {
            this.maxRadius = FindObjectOfType<MapComponent>()?.currentSystem?.size ?? 50;
            this.star = FindObjectOfType<StarLogic>().GetComponent<GravitySource>();
        }

        protected override (Result result, Vector2? newVelocity) UpdateManeuver(Vector2 aiPos, Vector2 aiVel)
        {
            var orbit = AnalyticOrbit.FromCartesianStateVector(aiPos, aiVel, this.star.parameters.mass,
                this.star.constants.GravitationalConstant);
            
            // this will catch escape trajectory as well
            if (orbit.isElliptic && orbit.nextapsis > this.maxRadius 
                || !orbit.isElliptic && !orbit.isDescending && aiPos.magnitude > this.maxRadius)
            {
                // var nextapsisVec = orbit.isUnstable
                //     ? (Vector3)aiVel
                //     : -orbit.GetNextapsisPosition() * orbit.directionSign;

                var clampedVec = -aiVel;
                // orbit.isUnstable
                //     ? OrbitManeuver.ApisThrustVector(aiVel, aiVel)
                //         : orbit.isElliptic 
                //             ? Vector2.Perpendicular(-orbit.GetNextapsisPosition().normalized * orbit.directionSign) 
                //             // OrbitManeuver.ApisThrustVector(aiVel, -orbit.GetNextapsisPosition() * orbit.directionSign)
                //             : -aiVel
                //     ;
                
                // var clampedVec = OrbitManeuver.ApisThrustVector(aiVel, nextapsisVec);
                Debug.DrawLine(aiPos, aiPos + clampedVec * 20, Color.blue, duration: 1f);
                return (Result.Running, clampedVec);
            }
            else
            {
                return (Result.Failure, null);
            }
        }
    }
    /// <summary>
    /// Intercepts a target if they are on a "global" path (one that includes more than one SOI).
    /// Otherwise it will follow the body the target is orbiting at a safe distance.
    /// </summary>
    private class InterceptTarget : AI.Behave.Node
    {
        private (BodyLogic b, Orbit o, GravitySource g)[] bodies;
        private readonly float shipFollowDistance;

        public InterceptTarget(float shipFollowDistance)
        {
            this.shipFollowDistance = shipFollowDistance;
            this.bodies = FindObjectsOfType<BodyLogic>().Select(b => (b, o: b.GetComponent<Orbit>(), g: b.GetComponent<GravitySource>())).ToArray();
        }
        
        // Version using orbit prediction with periapsis check
        public override Result Update(object blackboard)
        {
            var ai = (AIEnemyBehaviour)blackboard;
            
            var target = FindObjectOfType<PlayerController>().GetComponent<SimMovement>();
            var primary = FindObjectOfType<StarLogic>().GetComponent<GravitySource>();

            // Can't attack while docked... We could perhaps go any lie in wait though?
            if (target.GetComponent<DockActive>()?.docked ?? false)
            {
                return Result.Failure;
            }
            
            // We can only approach the target directly if they are in a "global" trajectory, i.e. not in orbit
            // around a planet/moon. Its fine if they are passing through multiple SOIs though as long as one of them is
            // the stars.
            (var targetPos, var targetVel, float followDistance) = this.GetTargetSpec(target, primary);
            
            ai.aiController.SetTargetVelocity(this.GetVelocityToIntercept((Vector2)ai.transform.position, (Vector2)ai.simMovement.velocity, primary, targetPos, targetVel, followDistance));

            return Result.Running;
        }

        private (Vector3 targetPos, Vector3 targetVel, float followDistance) GetTargetSpec(SimMovement target, GravitySource primary)
        {
            bool targetDirectlyApproachable = !target.sois.Any() || target.sois.Any(s => s.g == primary);
            if (targetDirectlyApproachable)
            {
                return (target.transform.position, target.velocity, this.shipFollowDistance);
            }
            else
            {
                var secondary = target.sois.First().g;
                var secondaryOrbit = secondary.GetComponent<Orbit>();

                // We want to trail outside of the SOI radius so we don't get dragged into the planet
                // TODO: maybe trail at L5? Its slow to catch the player if we do this...
                float soiRadius = OrbitalUtils.SecondarySOIRadius(secondaryOrbit.parameters.semiMajorAxis, secondary.parameters.mass, primary.parameters.mass, secondary.constants.GravitationalConstant, secondary.constants.GravitationalRescaling);

                return (secondaryOrbit.position.position, secondaryOrbit.absoluteVelocity, soiRadius * 5f);
            }
        }

        private Vector3 GetVelocityToIntercept(Vector3 aiPos, Vector3 aiVel, GravitySource primary, Vector3 targetPos, Vector3 targetVel, float trailDistance)
        {
            // If we are orbiting the same direction as the target we can directly intercept it
            // Otherwise we should reverse our orbit first

            // 1. determine orbits
            var ourOrbit = AnalyticOrbit.FromCartesianStateVector(
                aiPos, aiVel, 
                primary.parameters.mass, primary.constants.GravitationalConstant);
            var targetOrbit = AnalyticOrbit.FromCartesianStateVector(
                targetPos, targetVel, 
                primary.parameters.mass, primary.constants.GravitationalConstant);
            ourOrbit.DebugDraw(Vector3.zero, Color.cyan, duration: 2);
            targetOrbit.DebugDraw(Vector3.zero, Color.blue, duration: 2);

            // WIP
            //if(ourOrbit.periapsis <= )
            
            // if (ourOrbit.directionSign != targetOrbit.directionSign)
            // {
            //     // We aren't orbiting in the same direction, so change direction first
            //     
            // }
            // else
            {
                // We are in the same orbit so lets try and intercept
                
                // 2. calculate a suitable time-of-flight based on distance to target + its orbit characteristics (maybe)
                var tof = Vector3.Distance(aiPos, targetPos) * 0.5f;

                // 3. calculate the targets position at T+time-of-flight (this is the intercept point)
                // orbits with eccentricty near 1 are unstable in this system, we will just fly to the player instead of predicting position
                float trailTime = trailDistance / targetVel.magnitude;
                var interceptPos = targetOrbit.isUnstable ? (Vector2)targetPos : targetOrbit.GetPosition(tof - trailTime);

                // 4. solve lambert equation for transfer from current position to this position
                var (Vi, _) = GoodingSolver.Solve(primary.constants.GravitationalConstant * primary.parameters.mass,
                    aiPos, targetVel, interceptPos, tof, 0);
                Debug.DrawLine(aiPos, interceptPos, Color.cyan, duration: 2);
                return Vi;
            }
        }
    }
    
    private void Awake()
    {
        this.simMovement = this.GetComponent<SimMovement>();
        this.simulation = FindObjectOfType<Simulation>();
        this.aiController = this.GetComponent<AIController>();

        //this.rng = new RandomX();
        
        this.tree = new AI.Behave.Tree("AI enemy behaviour",
            new PeriodicUpdate(10,
                new AI.Behave.Selector("Priority selector",
                    // Avoid collisions as highest priority
                    new RaiseOrbit(5, 10),
                    new Sequence("Intercept maneuver",
                        // Raise orbit height before intercepting the target, to ensure we have some space to maneuver
                        new InvertResult(new RaiseOrbit(10, 20)),
                        new InterceptTarget(this.shipFollowDistance)
                        ),
                    new StayInSystem(),
                    new Idle()
                    )
                )
            );
    }
    
    public void SimUpdate(Simulation simulation, int simTick, int timeStep) => this.tree.Update(this);

    public void SimRefresh(Simulation simulation) {}

}
