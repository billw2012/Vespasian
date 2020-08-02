using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowCameraController : MonoBehaviour
{
    public float maxSpeed = 6.0f;

    [Tooltip("How much of a factor velocity is in the camera positioning"), Range(0f, 10f)]
    public float velocityScale = 2.0f;

    public float smoothTime = 10.0f;

    Vector3 offset;
    Vector3 offsetVelocity;
    Vector3 minCameraOffset;
    Vector3 maxCameraOffset;

    void Start()
    {
        var center = this.transform.position;
        var tr = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width * 0.8f, Screen.height * 0.8f, this.transform.position.z));
        this.minCameraOffset = center - tr;
        this.maxCameraOffset = tr - center;
    }

    static Vector3 Clamp(Vector3 value, Vector3 min, Vector3 max) =>
        Vector3.Max(min, Vector3.Min(value, max));

    void Update()
    {
        var follow = GameLogic.Instance.player.GetComponent<PlayerLogic>();
        var targetOffset = follow.velocity * velocityScale;

        targetOffset = Vector3.SmoothDamp(this.offset, targetOffset, ref this.offsetVelocity, this.smoothTime, this.maxSpeed);
        //var offsetDiff = targetOffset - this.offset;
        //var speed = Mathf.Min(this.maxSpeed, offsetDiff.magnitude);
        //var offsetChange = Vector3.ClampMagnitude(offsetDiff, speed * Time.deltaTime);
        //targetOffset = this.offset + offsetChange;

        var clampedPosition = Clamp(targetOffset, this.minCameraOffset, this.maxCameraOffset);
        // Don't modify the z coordinate (this is using swizzling to add a.xy and b.z)
        this.transform.position = (follow.transform.position + clampedPosition).xy0() + this.transform.position._00z();
        this.offset = clampedPosition;
    }
}
