using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetArea : MonoBehaviour
{
    // OnCollisionEnter is called when this collider/rigidbody has begun touching another rigidbody/collider
    void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject == GameLogic.Instance.player)
        {
            GameLogic.Instance.WinGame();
        }
    }
}
