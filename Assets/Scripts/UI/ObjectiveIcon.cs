using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class ObjectiveIcon : MonoBehaviour
{
    public Objective target;

    Image fill;
    Slider slider;

    void Start()
    {
        this.fill = this.transform.Find("Fill Area/Fill").GetComponent<Image>();
        this.slider = this.GetComponent<Slider>();
    }

    void Update()
    {
        this.slider.value = this.target.amountDone / this.target.amountRequired;
        if (this.target.complete)
        {
            this.fill.color = Color.green;
        }
    }
}
