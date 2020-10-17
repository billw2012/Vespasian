using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class ShieldBar : MonoBehaviour
{
    public PlayerController player;

    void Update()
    {
        var shield = this.player.GetComponentInChildren<ShieldComponent>();
        foreach(Transform child in this.transform)
        {
            child.gameObject.SetActive(shield != null);
        }
        if (shield != null)
        {
            this.GetComponent<Slider>().value = shield.shield;
        }
    }
}
