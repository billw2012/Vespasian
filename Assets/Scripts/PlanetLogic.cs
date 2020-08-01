using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlanetLogic : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // TODO: check for player proximity to deliver fuel/damage/aero breaking/etc.
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject == GameLogic.Instance.player)
        {
            GameLogic.Instance.LoseGame();
        }
    }
}
