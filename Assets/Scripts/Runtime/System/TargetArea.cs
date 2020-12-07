using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class TargetArea : MonoBehaviour
{
    public GameLogic gameLogic;

    private void OnValidate()
    {
        Assert.IsNotNull(this.gameLogic);
    }

    private void Start()
    {
        this.OnValidate();
    }

    // OnCollisionEnter is called when this collider/rigidbody has begun touching another rigidbody/collider
    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.GetComponentInParent<PlayerController>() != null)
        {
            //this.gameLogic.WinGame();
        }
    }
}
