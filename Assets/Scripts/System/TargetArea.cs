using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public class TargetArea : MonoBehaviour
{
    public GameLogic gameLogic;

    void OnValidate()
    {
        Assert.IsNotNull(this.gameLogic);
    }

    void Start()
    {
        this.OnValidate();
    }

    // OnCollisionEnter is called when this collider/rigidbody has begun touching another rigidbody/collider
    void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.GetComponentInParent<PlayerLogic>() != null)
        {
            this.gameLogic.WinGame();
        }
    }
}
