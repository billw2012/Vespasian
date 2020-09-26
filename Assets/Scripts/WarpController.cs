using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class WarpController : MonoBehaviour
{
    public enum Mode
    {
        Done,
        EnterWarp,
        TurnInWarp,
        ExitWarp
    }

    [NonSerialized]
    public Mode mode = Mode.Done;


    [NonSerialized]
    public float speed;

    public float warpSpeed = 100;
    public float acceleration = 25;


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
                if(this.speed < this.warpSpeed)
                {
                    this.speed = Mathf.Min(this.speed + this.acceleration * Time.deltaTime, this.warpSpeed);
                }
                if (Vector2.Distance(this.startPosition, this.transform.position) >= this.desiredDistance && this.speed >= this.warpSpeed)
                {
                    this.mode = Mode.Done;
                }
                break;
            case Mode.TurnInWarp:
                if (Vector2.Angle(this.direction, this.transform.up) < 5f)
                {
                    this.mode = Mode.Done;
                }
                break;
            case Mode.ExitWarp:
                this.speed = Mathf.Max(0, this.speed - this.acceleration * Time.deltaTime);

                if (this.speed <= this.desiredSpeed)
                {
                    this.mode = Mode.Done;
                }
                break;
        }

        float targetAngle = Vector2.SignedAngle(Vector2.up, this.direction);
        float newAngle = Mathf.SmoothDampAngle(this.transform.rotation.eulerAngles.z, targetAngle, ref this.rotationalSpeed, 3f);
        this.transform.rotation = Quaternion.Euler(0, 0, newAngle);
        this.transform.position += this.speed * Time.deltaTime * this.transform.up;

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
        
        await new WaitUntil(() => this.mode == Mode.Done);
    }

    public async Task TurnInWarpAsync(Vector2 direction)
    {
        this.mode = Mode.TurnInWarp;
        this.direction = direction;

        await new WaitUntil(() => this.mode == Mode.Done);
    }

    public async Task ExitWarpAsync(Vector2 atPosition, Vector2 direction, float finalSpeed)
    {
        this.mode = Mode.ExitWarp;
        this.direction = direction;
        //this.targetPosition = atPosition;
        this.rotationalSpeed = 0;
        float stoppingDistance = Mathf.Pow(this.speed - finalSpeed, 2) / (2f * this.acceleration);
        this.transform.position = atPosition - stoppingDistance * direction;
        this.desiredSpeed = finalSpeed;

        await new WaitUntil(() => this.mode == Mode.Done);
    }
}
