using UnityEngine;

public class SimpleBallisticMovement : MonoBehaviour
{
    public Vector2 velocity;

    private void FixedUpdate() => this.transform.position += (Vector3)this.velocity * Time.fixedDeltaTime;
}
