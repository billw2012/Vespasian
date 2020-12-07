using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

/*
 * After player has been there for more than specified time, it is triggered.
 * */

public class TargetAreaTimed : MonoBehaviour
{
    public GameLogic gameLogic;
    public GameObject player;
    public GameObject circle;

    public float duration = 1.0f;
    private float durationCurrent = 0;

    [Tooltip("Radius of the area"), Range(0.1f, 10)]
    public float radius = 1.0f;

    private SpriteRenderer spriteRenderer;

    private void OnValidate()
    {
        this.circle.transform.localScale = Vector3.one * 2.0f * this.radius;
    }

    private void Start()
    {
        Assert.IsNotNull(this.gameLogic);
        Assert.IsTrue(this.player != null, "Player is null");
        Assert.IsNotNull(this.circle);
        this.spriteRenderer = circle.GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (Vector3.Distance(transform.position, this.player.transform.position) < this.radius)
        {
            this.durationCurrent += Time.deltaTime;
            float progress = this.durationCurrent / this.duration;
            float red = Mathf.Clamp(1.0f - progress, 0, 1);
            float green = Mathf.Clamp(progress, 0, 1);
            this.spriteRenderer.color = new Color(red, green, 0);
            if (this.durationCurrent > this.duration)
            {
                //this.gameLogic.WinGame();
            }
        } else
        {
            this.durationCurrent = 0;
            this.spriteRenderer.color = Color.red;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(this.transform.position, this.radius);
    }
}
