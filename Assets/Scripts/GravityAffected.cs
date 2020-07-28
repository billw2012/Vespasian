using UnityEngine;

public class GravityAffected : MonoBehaviour {

    private void FixedUpdate() {
        var rigidBody = this.GetComponent<Rigidbody2D>();
        foreach(var src in GravitySource.All)
        {
            var vec = src.transform.position - this.transform.position;
            rigidBody.AddForce(GameConstants.Instance.AccelerationCoefficient * vec.normalized * src.Mass / Mathf.Pow(vec.magnitude, 2));
        }
    }
}