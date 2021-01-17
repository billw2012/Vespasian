// unset

using System.Linq;
using UnityEngine;

public class AIEnemyBehaviour : MonoBehaviour, ISimUpdate
{
    [SerializeField]
    private float shipFollowDistance = 3f;

    [SerializeField]
    private WeightedRandom updateInterval = null;

    private SimMovement simMovement;
    private AIController aiController;
    private int nextUpdateTick;
    private RandomX rng;
    private Vector3 interceptPos;
    private Simulation simulation;
    private AI.Behave.Tree tree;

    // // Possible AI states in priority order
    // private enum State
    // {
    //     AvoidCollision,
    //     AchieveSafeOrbit,
    //     ReverseOrbit,
    //     Intercept,
    //     //DirectApproach, <- Intercept does direct approach in rotational reference frame, which is correct
    //     //Follow, <- This is just Intercept to a point behind the target, which is already considered
    //     Idle,
    // }
    // private State state = State.Idle;
    
    // private interface IAIAction
    // {
    //     /// <summary>
    //     /// Should this state activate?
    //     /// </summary>
    //     /// <returns>true if this state should become or remain the active one</returns>
    //     bool ShouldActivate();
    //     /// <summary>
    //     /// Update the action
    //     /// </summary>
    //     void Update();
    // }

    // private class AvoidCollision : IAIAction
    // {
    //     public bool ShouldActivate() => throw new NotImplementedException();
    //
    //     public void Update() => throw new NotImplementedException();
    // }

    // We want to detect collision with other bodies and avoid it if possible
    // private class AvoidCollision : AI.Behave.Node
    // {
    //     private (BodyLogic b, Orbit o, GravitySource g)[] bodies;
    //     private float timeLookAhead;
    //     private float ownRadius;
    //
    //     public AvoidCollision(float timeLookAhead, float ownRadius)
    //     {
    //         this.timeLookAhead = timeLookAhead;
    //         this.ownRadius = ownRadius;
    //
    //         this.bodies = FindObjectsOfType<BodyLogic>().Select(b => (b, o: b.GetComponent<Orbit>(), g: b.GetComponent<GravitySource>())).ToArray();
    //     }
    //     
    //     // Version using dead reckoning
    //     public override Result Update(object blackboard)
    //     {
    //         var ai = (AIEnemyBehaviour)blackboard;
    //         var playerPos = (Vector2)ai.transform.position;
    //         var playerVel = (Vector2)ai.simMovement.velocity;
    //     
    //         Debug.DrawLine(playerPos, playerPos + playerVel * this.timeLookAhead, Color.red);
    //         // Detect intersection with bodies in our path, return vector relative to the body
    //         var r = this.bodies.Select(bog =>
    //         {
    //             // TODO: We need to vary the radius by proximity such that we deflect more
    //             // the closer we are to collision? We need to approximate orbit circularization, maybe check the 
    //             // proper solution to this?
    //             (bool occurred, var pos) = Geometry.IntersectLineSegmentCircle(
    //                 playerPos, playerPos + playerVel * this.timeLookAhead,
    //                 bog.b.geometry.position, bog.b.radius + this.ownRadius);
    //             return (occurred, bog.b, pos);
    //         }).FirstOrDefault(c => c.occurred);
    //         if (r.occurred)
    //         {
    //             Debug.DrawLine(playerPos, r.pos, Color.red);
    //     
    //             var collideVec = (r.pos - (Vector2)r.b.geometry.position).normalized;
    //             
    //             var lateralThrustVec = Vector2.Perpendicular(playerVel).normalized;
    //             var lateralThrust = (lateralThrustVec * Mathf.Sign(Vector2.Dot(collideVec, lateralThrustVec))).normalized;
    //             
    //             var reverseThrustVec = (-1 * playerVel).normalized;
    //             var reverseThrust = reverseThrustVec * ((Vector2.Dot(collideVec, reverseThrustVec) - 0.5f) * 0.5f);
    //             
    //             var tVec = lateralThrust + reverseThrust; // //(Vector3)(lateralThrustVec * Vector2.Dot(rv, lateralThrustVec)).normalized;
    //             
    //             ai.aiController.targetVelocity = playerVel + tVec * playerVel.magnitude; 
    //             Debug.DrawLine(playerPos, playerPos + tVec * 10, Color.blue);
    //             return Result.Running;
    //         }
    //         else
    //         {
    //             ai.aiController.targetVelocity = playerVel;
    //         }
    //         return Result.Failure;
    //     }
    // }
    
    private class Idle : AI.Behave.Node
    {
        public override Result Update(object blackboard)
        {
            var ai = (AIEnemyBehaviour)blackboard;
            ai.aiController.targetVelocity = (Vector2)ai.simMovement.velocity; 
            return Result.Success;
        }
    }
    
    // We want to detect collision with other bodies and avoid it if possible
    private class AvoidCollision : AI.Behave.Node
    {
        private (BodyLogic b, Orbit o, GravitySource g)[] bodies;
        private readonly float timeLookAhead;
        private readonly float safeRange;

        private Result state = Result.Failure;
        private int nextUpdateTick = 0;

        public AvoidCollision(float timeLookAhead, float safeRange)
        {
            this.timeLookAhead = timeLookAhead;
            this.safeRange = safeRange;

            this.bodies = FindObjectsOfType<BodyLogic>().Select(b => (b, o: b.GetComponent<Orbit>(), g: b.GetComponent<GravitySource>())).ToArray();
        }
        
        // Version using orbit prediction with periapsis check
        public override Result Update(object blackboard)
        {
            var ai = (AIEnemyBehaviour)blackboard;
            if (ai.simulation.simTick > this.nextUpdateTick)
            {
                this.nextUpdateTick = ai.simulation.simTick + 10; 
                
                var playerPos = (Vector2)ai.transform.position;
                var playerVel = (Vector2)ai.simMovement.velocity;

                Debug.DrawLine(playerPos, playerPos + playerVel * this.timeLookAhead, Color.red);
                // Find the closest body where we are in a descending orbit and the periapsis is too low
                var (b, orbit) = this.bodies
                    .OrderBy(bog => Vector2.Distance(playerPos, (Vector2)bog.o.position.position))
                    .Select(bog =>
                    {
                        // Orbit relative to this body
                        var playerRelativeVelocity = playerVel - (Vector2)bog.o.absoluteVelocity;
                        var playerRelativePosition = playerPos - (Vector2)bog.o.position.position;

                        return (bog.b, orbit: AnalyticOrbit.FromCartesianStateVector(
                            playerRelativePosition, playerRelativeVelocity,
                            bog.g.parameters.mass, bog.g.constants.GravitationalConstant));
                    })
                    .FirstOrDefault(bo =>
                        bo.orbit.isDescending && bo.orbit.periapsis < this.safeRange + bo.b.radius &&
                        bo.orbit.timeOfPeriapsis < 10);

                if (b != null)
                {
                    orbit.DebugDraw(b.geometry.position, Color.red, duration: 0.1f);

                    var periapsisVec = orbit.isUnstable ? (Vector3)playerVel : (-orbit.GetPeriapsisPosition() *
                        orbit.directionSign);

                    var tVec = Vector2.Perpendicular(periapsisVec.normalized);

                    ai.aiController.targetVelocity = playerVel + tVec * 20;
                    Debug.DrawLine(playerPos, playerPos + tVec * 20, Color.blue);
                    this.state = Result.Running;
                }
                else
                {
                    this.state = Result.Failure;
                }
            }

            return this.state;
        }
    }

    private void Awake()
    {
        this.simMovement = this.GetComponent<SimMovement>();
        this.simulation = FindObjectOfType<Simulation>();
        this.aiController = this.GetComponent<AIController>();
        this.aiController.targetVelocity = this.simMovement.startVelocity;

        this.rng = new RandomX();
        
        this.tree = new AI.Behave.Tree("AI enemy behaviour",
            new AI.Behave.Selector("",
                new AvoidCollision(10, 3),
                new Idle()
            )
        );
    }
    
    private void Update() => this.tree.Update(this);

    public void SimUpdate(Simulation simulation, int simTick, int timeStep)
    {
        // if (simTick >= this.nextUpdateTick)
        // {
        //     var target = FindObjectOfType<PlayerController>().GetComponent<SimMovement>();
        //     var primary = FindObjectOfType<StarLogic>().GetComponent<GravitySource>();
        //     
        //     // We can only approach the target directly if they are in a "global" trajectory, i.e. not in orbit
        //     // around a planet/moon. Its fine if they are passing through multiple SOIs though as long as one of them is
        //     // the stars.
        //     (var targetPos, var targetVel, float followDistance) = this.GetTargetSpec(target, primary);
        //     this.aiController.targetVelocity = this.GetVelocityToIntercept(primary, targetPos, targetVel, followDistance);
        //     this.nextUpdateTick +=  (int)(this.updateInterval.Evaluate(this.rng) / Time.fixedDeltaTime);
        // }
    }

    public void SimRefresh(Simulation simulation) {}

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

    private Vector3 GetVelocityToIntercept(GravitySource primary, Vector3 targetPos, Vector3 targetVel, float trailDistance)
    {
        // If we are orbiting the same direction as the target we can directly intercept it
        // Otherwise we should reverse our orbit first
        
        // 1. determine predicted target orbit
        var targetOrbit = AnalyticOrbit.FromCartesianStateVector(
            targetPos, targetVel, 
            primary.parameters.mass, primary.constants.GravitationalConstant);
        
        // 1b. determine our orbit
        var ourPosition = this.transform.position;
        var ourVelocity = this.simMovement.velocity;
        var ourOrbit = AnalyticOrbit.FromCartesianStateVector(
            ourPosition, ourVelocity, 
            primary.parameters.mass, primary.constants.GravitationalConstant);

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
            var tof = Vector3.Distance(ourPosition, targetPos) * 0.5f;

            // 3. calculate the targets position at T+time-of-flight (this is the intercept point)
            // orbits with eccentricty near 1 are unstable in this system, we will just fly to the player instead of predicting position
            float trailTime = trailDistance / targetVel.magnitude;
            this.interceptPos = targetOrbit.isUnstable ? (Vector2)targetPos : targetOrbit.GetPosition(tof - trailTime);

            // 4. solve lambert equation for transfer from current position to this position
            var (Vi, Vf) = GoodingSolver.Solve(primary.constants.GravitationalConstant * primary.parameters.mass,
                ourPosition, this.simMovement.velocity, this.interceptPos, tof, 0);

            return Vi;
        }
    }
}
