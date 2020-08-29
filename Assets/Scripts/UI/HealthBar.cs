using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class HealthBar : MonoBehaviour
{
    //public GameObject fill;
    public HealthComponent target;
    Slider slider;

    void Start()
    {
        Assert.IsNotNull(this.target);

        //Assert.IsNotNull(this.fill);
        this.slider = this.GetComponent<Slider>();
        Assert.IsNotNull(this.slider);
    }

    // Update is called once per frame
    void Update()
    {
        this.slider.value = this.target.health;
        //if (GameLogic.Instance.health < 0.2)
        //{
        //    this.fill.GetComponent<Image>().color = Color.red;
        //}
        //else if (GameLogic.Instance.health > 1)
        //{
        //    this.fill.GetComponent<Image>().color = new Color(0.2f, 1.0f, 0.2f);
        //}
    }
}
