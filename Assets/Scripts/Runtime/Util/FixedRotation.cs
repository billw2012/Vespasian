using UnityEngine;

public class FixedRotation : MonoBehaviour
{
    private void LateUpdate() => this.transform.rotation = Quaternion.identity;
}
