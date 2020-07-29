using System.Linq;
using UnityEngine;

public class GravityAffected : MonoBehaviour
{
    public Vector3 velocity = Vector3.zero;

    private static Vector3 CalculateForce(Vector3 from, GravitySource src)
    {
        var vec = src.transform.position - from;
        return GameConstants.Instance.GlobalCoefficient * GameConstants.Instance.AccelerationCoefficient * vec.normalized * src.Mass / Mathf.Pow(vec.magnitude, 2);
    }

    private void FixedUpdate()
    {
        // var rigidBody = this.GetComponent<Rigidbody2D>();
        // foreach(var src in GravitySource.All)
        // {
        //     var vec = src.transform.position - this.transform.position;
        //     rigidBody.AddForce(GameConstants.Instance.GlobalCoefficient * GameConstants.Instance.AccelerationCoefficient * vec.normalized * src.Mass / Mathf.Pow(vec.magnitude, 2));
        // }
        //if (enabled)
        //{
        var stepTime = Time.fixedDeltaTime / (float)GameConstants.Instance.PhysicsSteps;
        for (int i = 0; i < GameConstants.Instance.PhysicsSteps; i++)
        {
            var force = GravitySource.All
                .Select(src => CalculateForce(this.transform.position, src))
                .Aggregate((a, b) => a + b);
            velocity += force * stepTime;
            this.transform.position += velocity * stepTime;
        }
        //}
    }
}