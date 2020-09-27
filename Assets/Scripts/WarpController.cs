using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class WarpController : MonoBehaviour
{
    public enum Mode
    {
        NotInWarp,
        EnterWarp,
        AtWarp,
        TurnInWarp,
        ExitWarp
    }

    [NonSerialized]
    public Mode mode = Mode.NotInWarp;


    [NonSerialized]
    public float speed;

    public float warpSpeed = 100;
    public float acceleration = 25;

    public WarpEffect warpEffect;

    public Background background;
    public StarField starfield;

    public FollowCameraController cameraController;

    float rotationalSpeed;

    // For EnterWarp
    Vector2 startPosition;
    float desiredDistance;

    // For ExitWarp
    float desiredSpeed;
    Vector2 direction;

    // Update is called once per frame
    void FixedUpdate()
    {
        switch (this.mode)
        {
            case Mode.EnterWarp:
                if (this.speed < this.warpSpeed)
                {
                    this.speed = Mathf.Min(this.speed + this.acceleration * Time.deltaTime, this.warpSpeed);
                }
                if (Vector2.Distance(this.startPosition, this.transform.position) >= this.desiredDistance && this.speed >= this.warpSpeed)
                {
                    this.mode = Mode.AtWarp;
                }
                break;
            case Mode.TurnInWarp:
                if (Vector2.Angle(this.direction, this.transform.up) < 5f)
                {
                    this.mode = Mode.AtWarp;
                }
                break;
            case Mode.ExitWarp:
                this.speed = Mathf.Max(0, this.speed - this.acceleration * Time.deltaTime);

                if (this.speed <= this.desiredSpeed)
                {
                    this.mode = Mode.NotInWarp;
                }
                break;
        }

        float targetAngle = Vector2.SignedAngle(Vector2.up, this.direction);
        float newAngle = Mathf.SmoothDampAngle(this.transform.rotation.eulerAngles.z, targetAngle, ref this.rotationalSpeed, 3f);
        float angleChange = (newAngle - this.transform.rotation.eulerAngles.z) / Time.deltaTime;
        this.transform.rotation = Quaternion.Euler(0, 0, newAngle);
        this.transform.position += this.speed * Time.deltaTime * this.transform.up;

        this.warpEffect.amount = this.mode == Mode.NotInWarp ? 0 : Mathf.InverseLerp(this.warpSpeed * 0.5f, this.warpSpeed, this.speed);
        this.warpEffect.turningAmount = Mathf.Clamp(-angleChange / 360f, -0.05f, 0.05f);
        this.warpEffect.direction = this.transform.up;


        this.starfield.fade = this.mode == Mode.NotInWarp ? 1 : 1 - Mathf.InverseLerp(this.warpSpeed * 0.5f, this.warpSpeed * 0.75f, this.speed);

        //var rigidBody = this.GetComponent<Rigidbody>();
        //rigidBody.MoveRotation(Quaternion.Euler(0, 0, newAngle));
        //rigidBody.MovePosition(rigidBody.position + this.speed * Time.deltaTime * this.transform.up);
    }

    public async Task EnterWarpAsync(Vector2 direction, float desiredDistance)
    {
        this.mode = Mode.EnterWarp;
        this.direction = direction;
        this.rotationalSpeed = 0;
        this.speed = 0;
        this.startPosition = this.transform.position;
        this.desiredDistance = desiredDistance;
        
        await new WaitUntil(() => this.mode == Mode.AtWarp);
    }

    public async Task TurnInWarpAsync(Vector2 direction)
    {
        this.mode = Mode.TurnInWarp;
        this.direction = direction;

        await new WaitUntil(() => this.mode == Mode.AtWarp);
    }

    public async Task ExitWarpAsync(Vector2 atPosition, Vector2 direction, float finalSpeed)
    {
        this.mode = Mode.ExitWarp;
        this.direction = direction;
        //this.targetPosition = atPosition;
        this.rotationalSpeed = 0;
        float stoppingDistance = Mathf.Pow(this.speed - finalSpeed, 2) / (2f * this.acceleration);

        var oldPosition = this.transform.position;
        this.transform.position = atPosition - stoppingDistance * direction;

        var positionOffset = this.transform.position - oldPosition;

        this.starfield.ApplyPositionOffset(positionOffset);
        this.background.ApplyPositionOffset(positionOffset);

        this.desiredSpeed = finalSpeed;

        this.cameraController.Update();

        await new WaitUntil(() => this.mode == Mode.NotInWarp);
    }
}
