using System.Threading.Tasks;
using UnityEngine;

public class WarpController : MonoBehaviour
{
    public float warpSpeed = 100;
    public float acceleration = 25;

    public WarpEffect warpEffect;

    public Background background;
    public StarField starfield;

    public FollowCameraController cameraController;

    public PostEffect warpPostEffect;

    private enum Mode
    {
        NotInWarp,
        EnterWarp,
        AtWarp,
        TurnInWarp,
        ExitWarp
    }

    private Mode mode = Mode.NotInWarp;

    private float speed;

    private float rotationalSpeed;

    // For EnterWarp
    private Vector2 startPosition;
    private float minDistanceBeforeWarp;

    // For ExitWarp
    private float desiredSpeed;
    private float distanceToExit;
    private Vector2 direction;
    private Vector2 targetPosition;

    private void Start()
    {
        this.warpPostEffect.Init();
    }

    // Update is called once per frame
    private void FixedUpdate()
    {
        float SimulatedUpdate()
        {
            float targetAngle = Vector2.SignedAngle(Vector2.up, this.direction);
            float newAngle =
                Mathf.SmoothDampAngle(this.transform.rotation.eulerAngles.z, targetAngle, ref this.rotationalSpeed, 3f);
            float f = (newAngle - this.transform.rotation.eulerAngles.z) / Time.deltaTime;
            this.transform.rotation = Quaternion.Euler(0, 0, newAngle);
            this.transform.position += this.speed * Time.deltaTime * this.transform.up;
            return f;
        }
        
        void InterpolatedUpdate()
        {
            // float targetAngle = Vector2.SignedAngle(Vector2.up, this.direction);
            // float newAngle =
            //     Mathf.SmoothDampAngle(this.transform.rotation.eulerAngles.z, targetAngle, ref this.rotationalSpeed, 3f);
            // float f = (newAngle - this.transform.rotation.eulerAngles.z) / Time.deltaTime;
            var targetDir = (this.targetPosition - (Vector2)this.transform.position).normalized;
            this.transform.rotation = Quaternion.Euler(0, 0, Vector2.SignedAngle(Vector2.up, targetDir));
            this.transform.position += this.speed * Time.deltaTime * (Vector3)targetDir;
            //return f;
        }

        float angleChange = 0;
        switch (this.mode)
        {
            case Mode.EnterWarp:
                if (this.speed < this.warpSpeed)
                {
                    this.speed = Mathf.Min(this.speed + this.acceleration * Time.deltaTime, this.warpSpeed);
                }

                float distance = Vector2.Distance(this.startPosition, this.transform.position);
                if (distance >= this.minDistanceBeforeWarp && this.speed >= this.warpSpeed)
                {
                    Debug.Log($"Entered warp at distance {distance} speed {this.speed}");
                    this.mode = Mode.AtWarp;
                }
                
                angleChange = SimulatedUpdate();
                break;
            case Mode.TurnInWarp:
                if (Vector2.Angle(this.direction, this.transform.up) < 5f)
                {
                    this.mode = Mode.AtWarp;
                }
                
                angleChange = SimulatedUpdate();
                break;
            case Mode.ExitWarp:
                this.speed = Mathf.Max(this.desiredSpeed, this.speed - this.acceleration * Time.deltaTime);
                InterpolatedUpdate();
                this.distanceToExit -= this.speed * Time.deltaTime;
                if (this.distanceToExit <= 0) //Vector2.Distance(this.transform.position, this.targetPosition) < 1)//this.speed <= this.desiredSpeed)
                {
                    Debug.Log($"Exited warp at pos {this.transform.position} dir {this.direction} speed {this.speed}");
                    this.mode = Mode.NotInWarp;
                }

                break;
        }

        float warpEffectAmount = this.mode == Mode.NotInWarp ? 0 : Mathf.InverseLerp(this.warpSpeed * 0.5f, this.warpSpeed, this.speed);
        this.warpEffect.amount = warpEffectAmount;
        this.warpEffect.turningAmount = Mathf.Clamp(-angleChange / 360f, -0.05f, 0.05f);
        this.warpEffect.direction = this.transform.up;

        this.warpPostEffect.Update(warpEffectAmount);

        this.starfield.fade = this.mode == Mode.NotInWarp ? 1 : 1 - Mathf.InverseLerp(this.warpSpeed * 0.5f, this.warpSpeed * 0.75f, this.speed);
    }

    public async Task EnterWarpAsync(Vector2 direction, float minDistanceBeforeWarp)
    {
        Debug.Log($"Requested enter warp at dir {direction} min distance {minDistanceBeforeWarp}");
        
        this.mode = Mode.EnterWarp;
        this.direction = direction;
        this.rotationalSpeed = 0;
        this.speed = 0;
        this.startPosition = this.transform.position;
        this.minDistanceBeforeWarp = minDistanceBeforeWarp;
        
        await new WaitUntil(() => this.mode == Mode.AtWarp);
    }

    public async Task TurnInWarpAsync(Vector2 direction)
    {
        Debug.Log($"Requested turn in warp to dir {direction}");
        
        this.mode = Mode.TurnInWarp;
        this.direction = direction;

        await new WaitUntil(() => this.mode == Mode.AtWarp);
    }

    public async Task ExitWarpAsync(Vector2 atPosition, float finalSpeed)
    {
        Debug.Log($"Requested exit warp at {atPosition} speed {finalSpeed}");

        this.mode = Mode.ExitWarp;
        //this.direction = direction;
        this.targetPosition = atPosition;
        this.rotationalSpeed = 0;
        
        float stoppingDistance = Mathf.Pow(this.speed - finalSpeed, 2) / (2f * this.acceleration);

        var oldPosition = this.transform.position;
        this.transform.position = atPosition - stoppingDistance * this.direction;

        var positionOffset = this.transform.position - oldPosition;

        this.starfield.ApplyPositionOffset(positionOffset);
        this.background.ApplyPositionOffset(positionOffset);

        this.desiredSpeed = finalSpeed;
        this.distanceToExit = stoppingDistance;

        this.cameraController.Update();

        await new WaitUntil(() => this.mode == Mode.NotInWarp);
    }
}
