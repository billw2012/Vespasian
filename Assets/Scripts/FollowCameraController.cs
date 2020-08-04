using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowCameraController : MonoBehaviour
{
    //[Tooltip("How fast the camera can move to its desired offset"), Range(0f, 100f)]
    //public float maxSpeed = 60.0f;

    [Tooltip("How much of a factor velocity is in the camera positioning"), Range(0f, 10f)]
    public float velocityScale = 2.0f;

    [Tooltip("How fast the camera can move to its desired offset"), Range(0f, 10f)]
    public float smoothTime = 1.0f;

    Vector3 offset;
    Vector3 offsetVelocity;
    Rect cameraArea;

    void Start()
    {
        var center = this.transform.position;
        var tr = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width * 0.8f, Screen.height * 0.8f, this.transform.position.z));
        this.cameraArea = new Rect(center - tr, (tr - center) * 2);
    }

    void Update()
    {
        var follow = GameLogic.Instance.player.GetComponent<PlayerLogic>();
        var targetOffset = follow.velocity * velocityScale;

        targetOffset = Vector3.SmoothDamp(this.offset, targetOffset, ref this.offsetVelocity, this.smoothTime);
        var maxOffset = this.cameraArea.IntersectionWithRayFromCenter(targetOffset);
        var clampedOffset = Vector3.ClampMagnitude(targetOffset, maxOffset.magnitude);
        // Don't modify the z coordinate
        this.transform.position = (follow.transform.position + clampedOffset).xy0() + this.transform.position._00z();
        this.offset = clampedOffset;
    }
}
