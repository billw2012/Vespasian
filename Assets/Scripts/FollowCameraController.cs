using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowCameraController : MonoBehaviour
{
    [Tooltip("Time it takes to interpolate camera position 99% of the way to the target."), Range(0.001f, 1f)]
    public float positionLerpTime = 1f;

    public float maxSpeed = 0.1f;

    [Tooltip("How much of a factor velocity is in the camera positioning"), Range(0f, 100f)]
    public float velocityScale = 1.0f;


    Vector3 offset;
    Vector3 minCameraOffset;
    Vector3 maxCameraOffset;


    void Start()
    {
        var center = Camera.main.transform.position;
        var tr = this.gameObject.GetComponent<Camera>().ScreenToWorldPoint(new Vector3(Screen.width * 0.8f, Screen.height * 0.8f, Camera.main.transform.position.z));
        this.minCameraOffset = center - tr;
        this.maxCameraOffset = tr - center;
    }

    //static Vector3 Lerp(Vector3 from, Vector3 to, float t) => from + (to - from) * t;
    //static Vector2 Lerp(Vector2 from, Vector2 to, float t) => from + (to - from) * t;

    static Vector3 Clamp(Vector3 value, Vector3 min, Vector3 max) =>
        Vector3.Max(min, Vector3.Min(value, max));

    void Update()
    {
        var follow = GameLogic.Instance.player.GetComponent<PlayerLogic>();
        // var frac = 1f - Mathf.Exp((Mathf.Log(1f - 0.99f) / this.positionLerpTime) * Time.deltaTime);
        //var currOffset = this.transform.position - follow.transform.position;
        var targetOffset = follow.velocity * velocityScale;

        // Only do interpolation when the player is moving
        //if (follow.enabled)
        {
            var offsetChange = Vector3.ClampMagnitude(targetOffset - this.offset, this.maxSpeed * Time.deltaTime);
            targetOffset = this.offset + offsetChange; // Lerp(currOffset, targetOffset, frac);
        }
        var clampedOffset = Clamp(targetOffset, this.minCameraOffset, this.maxCameraOffset);
        clampedOffset.z = 0;
        this.transform.position = follow.transform.position + clampedOffset;
        this.offset = clampedOffset;

        //var targetPos = follow.transform.position + clampedOffset;
        //var move = (targetOffset - currOffset) * frac;
        //var maxDist = maxSpeed * Time.deltaTime;
        //var dist = Mathf.Min(move.magnitude, maxDist);
        //this.transform.position = this.transform.position + move.normalized * dist;
    }
}
