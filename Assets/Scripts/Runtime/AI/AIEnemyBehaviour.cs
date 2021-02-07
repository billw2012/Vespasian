using AI.Behave;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class AIEnemyBehaviour : MonoBehaviour, ISimUpdate
{
    [SerializeField]
    private float shipFollowDistance = 3f;

    private SimMovement simMovement;
    private AIController aiController;
    private Simulation simulation;
    private AI.Behave.Tree tree;

    private GameObject target;
    
    public bool debug = false;

    /// <summary>
    /// Sets target velocity to current velocity
    /// </summary>
    private class Idle : Node
    {
        protected override (Result result, Node node) UpdateImpl(object blackboard)
        {
            var ai = (AIEnemyBehaviour)blackboard;
            ai.aiController.DisableTargetVelocity(); 
            return (Result.Success, this);
        }
    }

    /// <summary>
    /// Wraps any node to only call its Update periodically instead of every update.
    /// Between Updates this node returns the last value.
    /// </summary>
    private class PeriodicUpdate : Decorator
    {
        private readonly int intervalSimTicks;
        //private Result state = Result.Failure;
        private int nextUpdateTick = 0;

        public PeriodicUpdate(int intervalSimTicks, Node child) : base(child)
        {
            this.intervalSimTicks = intervalSimTicks;
        }

        protected override (Result result, Node node) UpdateImpl(object blackboard)
        {
            var ai = (AIEnemyBehaviour)blackboard;
            if (ai.simulation.simTick > this.nextUpdateTick)
            {
                this.nextUpdateTick = ai.simulation.simTick + this.intervalSimTicks;
                (this.lastResult, this.lastNode) = this.child.Update(blackboard);
            }
            return (this.lastResult, this.lastNode);
        }
    }

    private class InvertResult : Decorator
    {
        public InvertResult(Node child) : base(child) {}

        protected override (Result result, Node node) UpdateImpl(object blackboard)
        {
            var (result, node) = this.child.Update(blackboard);
            if (result == Result.Failure)
            {
                return (Result.Success, node);
            }
            else if (result == Result.Success)
            {
                return (Result.Failure, node);
            }
            return (result, node);
        }
    }
    
    private abstract class OrbitManeuver : Node
    {
        public struct BOG
        {
            public BodyLogic body;
            public Orbit orbit;
            public GravitySource gravitySource;
            
            public bool Equals(BOG other) => Equals(this.body, other.body) && Equals(this.orbit, other.orbit) && Equals(this.gravitySource, other.gravitySource);
            public override bool Equals(object obj) => obj is BOG other && this.Equals(other);
            public static bool operator ==(BOG left, BOG right) => left.Equals(right);
            public static bool operator !=(BOG left, BOG right) => !left.Equals(right);
            public override int GetHashCode()
            {
                unchecked
                {
                    int hashCode = (this.body != null ? this.body.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ (this.orbit != null ? this.orbit.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ (this.gravitySource != null ? this.gravitySource.GetHashCode() : 0);
                    return hashCode;
                }
            }
            
        }
        
        protected readonly BOG[] bodies;
        protected readonly BOG primary;

        protected OrbitManeuver()
        {
            this.bodies = FindObjectsOfType<BodyLogic>()
                .Select(b => new BOG
                {
                    body = b,
                    orbit = b.GetComponent<Orbit>(),
                    gravitySource = b.GetComponent<GravitySource>(),
                })
                .ToArray();
            this.primary = this.bodies.FirstOrDefault(b => b.body.GetComponent<StarLogic>()?.isPrimary ?? false);
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
        protected IEnumerable<(BOG bog, AnalyticOrbit orbit)> GetOrbits(Vector2 pos, Vector2 vel) =>
            this.bodies
                .OrderBy(bog => Vector2.Distance(pos, (Vector2)bog.orbit.position.position))
                .Select(bog =>
                {
                    // Orbit relative to this body
                    var relVelocity = vel - (Vector2)bog.orbit.absoluteVelocity;
                    var relPosition = pos - (Vector2)bog.orbit.position.position;

                    return (bog, orbit: AnalyticOrbit.FromCartesianStateVector(
                        relPosition, relVelocity,
                        bog.gravitySource.parameters.mass, bog.gravitySource.constants.GravitationalConstant));
                });

        protected override (Result result, Node node) UpdateImpl(object blackboard)
        {
            var ai = (AIEnemyBehaviour)blackboard;
            
            var aiPos = (Vector2)ai.transform.position;
            var aiVel = (Vector2)ai.simMovement.velocity;

            var (result, newVelocity) = this.UpdateManeuver(aiPos, aiVel);
            if(newVelocity.HasValue)
            {
                ai.aiController.SetTargetVelocity(aiVel + newVelocity.Value * 20);
            }

            return (result, this);
        }

        protected static Vector2 ApsisThrustVector(Vector2 velocity, Vector2 apsisVec)
        {
            var tVec = Vector2.Perpendicular(apsisVec.normalized);
            var forward = velocity.normalized;
            var sideways = Vector2.Perpendicular(forward);
            // Exclude any backwards thrust as it makes no sense when trying to raise an apsis (player velocity is always forward)
            var forwardComponent = forward * Mathf.Max(0, Vector2.Dot(forward, tVec));
            var sideComponent = sideways * Mathf.Sign(Vector2.Dot(sideways, tVec));
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
        private readonly Func<BOG, AnalyticOrbit, float> minHeightFn;
        
        public RaiseOrbit(Func<BOG, AnalyticOrbit, float> minHeightFn, float timeLookAhead = float.MaxValue) : base()
        {
            this.minHeightFn = minHeightFn;
            this.timeLookAhead = timeLookAhead;
        }

        protected override (Result result, Vector2? newVelocity) UpdateManeuver(Vector2 aiPos, Vector2 aiVel)
        {
            // Debug.DrawLine(aiPos, aiPos + aiVel * this.timeLookAhead, Color.red, duration: 1f);

            // Find the closest body where we are in a descending orbit and the orbit is too low
            var (bog, orbit) = this.GetOrbits(aiPos, aiVel)
                .FirstOrDefault(bogo =>
                {
                    float finalHeight = this.minHeightFn(bogo.bog, bogo.orbit) + bogo.bog.body.radius;
                    return bogo.orbit.nextapsis < finalHeight &&
                           bogo.orbit.timeOfNextapsis > 0 && bogo.orbit.timeOfNextapsis <
                           this.timeLookAhead + finalHeight / aiVel.magnitude;
                });

            if (bog != default)
            {
                //orbit.DebugDraw(bog.body.geometry.position, Color.red, duration: 1f);

                var nextapsisVec = orbit.isUnstable
                    ? aiVel
                    : (Vector2)(-orbit.GetNextapsisPosition()) * orbit.directionSign;
                
                //Debug.DrawLine(aiPos, aiPos + nextapsisVec * 30, Color.magenta, duration: 0.1f);
                //Debug.Log($"{nextapsisVec}");
                //Debug.DrawLine(aiPos, aiPos + Vector2.Perpendicular(nextapsisVec) * 3, Color.cyan, duration: 0.1f);
                //Debug.DrawLine(aiPos, aiPos + aiVel.normalized * 3, Color.red, duration: 0.1f);
                var clampedVec = ApsisThrustVector(aiVel, nextapsisVec);
                //Debug.DrawLine(aiPos, aiPos + clampedVec * 20, Color.blue, duration: 0.1f);
                return (Result.Running, clampedVec);
            }
            else
            {
                return (Result.Failure, null);
            }
        }
    }
    
    /// <summary>
    /// Coast until at least minHeight above surface of primary 
    /// </summary>
    private class CoastUntilHeight : Node
    {
        private readonly float minHeight;
        private readonly BodyLogic primary;
        
        public CoastUntilHeight(float minHeight)
        {
            this.minHeight = minHeight;
            this.primary = FindObjectOfType<MapComponent>()?.primary?.GetComponent<BodyLogic>();
        }

        protected override (Result result, Node node) UpdateImpl(object blackboard)
        {
            var ai = (AIEnemyBehaviour)blackboard;
            
            var aiPos = (Vector2)ai.transform.position;
            if (this.primary == default)
            {
                // Height doesn't mean anything if there is no primary
                return (Result.Success, this);
            }
            return (aiPos.magnitude > this.minHeight + this.primary.radius ? Result.Success : Result.Running, this);
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
            var mapComponent = FindObjectOfType<MapComponent>();
            this.maxRadius = mapComponent?.currentSystem?.size ?? 50;
            this.star = mapComponent?.primary?.GetComponent<GravitySource>();
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
                //Debug.DrawLine(aiPos, aiPos + clampedVec * 20, Color.blue, duration: 1f);
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
    private class InterceptTarget : Node
    {
        private readonly float shipFollowDistance;
        private GravitySource primary;

        public InterceptTarget(float shipFollowDistance)
        {
            this.shipFollowDistance = shipFollowDistance;
            //this.player = FindObjectOfType<PlayerController>();
            this.primary = FindObjectOfType<MapComponent>()?.primary.GetComponent<GravitySource>();
        }
        
        // Version using orbit prediction with periapsis check
        protected override (Result result, Node node) UpdateImpl(object blackboard)
        {
            var ai = (AIEnemyBehaviour)blackboard;

            // Can't attack if player doesn't exist
            if (ai.target == null)
            {
                return (Result.Failure, this);
            }
            
            // Can't attack while docked... We could perhaps go any lie in wait though?
            var dockActive = ai.target.GetComponent<DockActive>();
            if (dockActive != null && dockActive.docked)
            {
                return (Result.Failure, this);
            }
            
            // We can only approach the target directly if they are in a "global" trajectory, i.e. not in orbit
            // around a planet/moon. Its fine if they are passing through multiple SOIs though as long as one of them is
            // the stars.
            (var targetPos, var targetVel, float followDistance) 
                = this.GetTargetSpec(ai.target.GetComponent<SimMovement>(), this.primary);
            
            ai.aiController.SetTargetVelocity(this.GetVelocityToIntercept((Vector2)ai.transform.position, (Vector2)ai.simMovement.velocity, this.primary, targetPos, targetVel, followDistance));

            return (Result.Running, this);
        }

        private (Vector3 targetPos, Vector3 targetVel, float followDistance) GetTargetSpec(SimMovement target, GravitySource primary)
        {
            bool targetDirectlyApproachable = !target.sois.Any() || target.sois.FirstOrDefault()?.g == primary;
            if (targetDirectlyApproachable)
            {
                return (target.transform.position, target.velocity, this.shipFollowDistance);
            }
            else
            {
                GravitySource GetSecondary(GravitySource g)
                {
                    for(;;)
                    {
                        var p = g.GetComponentInParentOnly<GravitySource>();
                        if (p == null || (p.GetComponent<StarLogic>()?.isPrimary ?? false))
                        {
                            return g;
                        }   
                        g = p;
                    }
                }
                
                var secondary = GetSecondary(target.sois.First().g);
                var secondaryOrbit = secondary.GetComponent<Orbit>();

                // We want to trail outside of the SOI radius of the secondary (planet not moon)
                // so we don't get dragged into the planet
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
                var tof = MathX.TimeToCoverDistance(1f, Vector3.Distance(aiPos, targetPos) * 0.5f); 
                    //Vector3.Distance(aiPos, targetPos) * 0.5f;

                // 3. calculate the targets position at T+time-of-flight (this is the intercept point)
                // orbits with eccentricty near 1 are unstable in this system, we will just fly to the player instead of predicting position
                float trailTime = trailDistance / targetVel.magnitude;
                var interceptPos = targetOrbit.isUnstable ? (Vector2)targetPos : targetOrbit.GetPosition(tof - trailTime);

                // 4. solve lambert equation for transfer from current position to this position
                var (Vi, _) = GoodingSolver.Solve(primary.constants.GravitationalConstant * primary.parameters.mass,
                    aiPos, targetVel, interceptPos, tof, 0);
                Debug.DrawLine(aiPos, interceptPos, Color.red, duration: 2);
                var interceptOrbit = AnalyticOrbit.FromCartesianStateVector(
                    aiPos, Vi, 
                    primary.parameters.mass, primary.constants.GravitationalConstant);
                interceptOrbit.DebugDraw(Vector3.zero, Color.red, duration: 2);
                return Vi;
                
                //TODO: avoidance requirements can be reduced if intercept orbits are more reliable, e.g. fix orbit direction, ensure duration allows keeping peri high, or just enforce peri
                // might need to compare phases?
            }
        }
    }
    
    private void Start()
    {
        this.simMovement = this.GetComponent<SimMovement>();
        this.simulation = FindObjectOfType<Simulation>();
        this.aiController = this.GetComponent<AIController>();

        //this.rng = new RandomX();
        
#if UNITY_EDITOR
        Debug.Log(string.Join(",", Font.GetOSInstalledFontNames()));
        this._debugStyle = new GUIStyle
        {
            font = Font.CreateDynamicFontFromOSFont("Consolas", 12),
            normal = new GUIStyleState
            {
                textColor = Color.white
            }
            //Resources.GetBuiltinResource<Font>("consola.ttf")
        };
#endif
    }
    
    public void SimUpdate(Simulation simulation, int simTick, int timeStep) => this.tree?.Update(this);
    
    public void SimRefresh(Simulation simulation)
    {
        this.target = FindObjectOfType<PlayerController>().gameObject;

        this.tree = new AI.Behave.Tree("AI enemy behaviour",
            new PeriodicUpdate(60,
                new Selector("Priority selector",
                    // Avoid collisions as highest priority
                    new RaiseOrbit((bog, o) => 20f + bog.orbit.absoluteVelocity.magnitude * 3f, 10),
                    new Sequence("Intercept maneuver",
                        // Raise orbit height before intercepting the target, to ensure we have some space to maneuver
                        new InvertResult(new RaiseOrbit((_, __) => 30, 20)),
                        new CoastUntilHeight(25),
                        new InterceptTarget(this.shipFollowDistance)
                    ),
                    new StayInSystem(),
                    new Idle()
                )
            )
        );
    }

    #if UNITY_EDITOR
    private GUIStyle _debugStyle;
    private void OnDrawGizmos()
    {
        if (this.tree == null)
        {
            return;
        }
        var labels = new List<string>{ $"{this.gameObject.name}" };
        this.tree.Visit((node, depth) =>
        {
            labels.Add(String.Empty.PadLeft(depth * 2, ' ')
                       + node.name.PadRight(24 - depth * 2)
                       + node.lastResult.ToString().PadRight(10)
                       + node.tick.ToString().PadRight(10)
            );
            return node != this.tree.lastNode;
        });
        Handles.color = Color.white;
        Handles.Label(this.transform.position, string.Join("\n", labels), this._debugStyle);
    }
    #endif
}
