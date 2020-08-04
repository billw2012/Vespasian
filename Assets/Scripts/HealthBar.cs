using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public GameObject fill;

    // Update is called once per frame
    void Update()
    {
        Debug.Assert(this.fill != null);
        var slider = this.GetComponent<Slider>();
        Debug.Assert(slider != null);

        slider.value = GameLogic.Instance.health;
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
