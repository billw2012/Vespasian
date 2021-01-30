using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class FuelTankBar : MonoBehaviour
{
    public FuelTankComponent fuelTank;

    [SerializeField]
    private Slider mainSlider = null;
    [SerializeField]
    private Slider usageSlider = null;
    [SerializeField]
    private float unitsPerFuel = 100;
    
    private EngineController engineController;
    private MapComponent map;

    private void Start()
    {
        this.engineController = FindObjectOfType<PlayerController>().GetComponent<EngineController>();
        this.map = FindObjectOfType<MapComponent>();
    }

    private void Update()
    {
        if (this.fuelTank != null)
        {
            ((RectTransform)this.mainSlider.transform).SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, this.fuelTank.maxFuel * this.unitsPerFuel);
            this.mainSlider.maxValue = this.fuelTank.maxFuel;
            this.mainSlider.value = this.fuelTank.fuel;
            this.usageSlider.maxValue = this.fuelTank.fuel;
            // Get the jump fuel requirements for this specific tank
            float jumpFuelUsage = this.engineController.GetFuelTankUsage(this.fuelTank, this.map?.GetJumpFuelRequired() ?? 0);
            this.usageSlider.value = this.fuelTank.fuel - Mathf.Clamp(jumpFuelUsage, 0, this.fuelTank.fuel);
        }
        else
        {
            this.mainSlider.value = this.usageSlider.value = 0;
        }
    }
}
