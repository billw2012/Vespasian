using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowCameraController : MonoBehaviour
{
    public float maxSpeed = 0.1f;

    [Tooltip("How much of a factor velocity is in the camera positioning"), Range(0f, 10f)]
    public float velocityScale = 1.0f;

    Vector3 offset;
    Vector3 minCameraOffset;
    Vector3 maxCameraOffset;

    void Start()
    {
        var center = Camera.main.transform.position;
        var tr = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width * 0.8f, Screen.height * 0.8f, Camera.main.transform.position.z));
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
        var targetOffset = follow.velocity * velocityScale;

        var offsetChange = Vector3.ClampMagnitude(targetOffset - this.offset, this.maxSpeed * Time.deltaTime);
        targetOffset = this.offset + offsetChange;

        var clampedPosition = Clamp(targetOffset, this.minCameraOffset, this.maxCameraOffset);
        clampedPosition.z = 0;
        this.transform.position = follow.transform.position + clampedPosition;
        this.offset = clampedPosition;
    }
}
