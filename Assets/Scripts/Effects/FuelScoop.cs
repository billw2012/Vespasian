using UnityEngine;

public class FuelScoop : MonoBehaviour
{
    public ParticleSystem particleEffect;

    private void Update()
    {
        var fuelSource = EffectSource.GetNearest<FuelSource>(this.transform);
        ///Debug.Log($"Nearest fuel source: {fuelSource}");
        if (fuelSource != null)
        {
            var playerLogic = GetComponent<PlayerLogic>();
            if (playerLogic != null)
            {
                float fuelIncrease = Time.deltaTime * 0.3f;
                playerLogic.AddFuel(fuelIncrease);
            }

            // Set emission amount based on height ratio
            this.particleEffect.SetEmissionEnabled(true);
            float effectStrength = fuelSource.GetEffectStrengthNormalized(this.transform);
            particleEffect.SetEmissionRateOverTimeMultiplier(50.0f * Mathf.Clamp(0.05f + 1.0f * effectStrength, 0, 1));

            // Check if the source is on the left or right, flip particle effect if needed
            var offset = fuelSource.transform.position - transform.position;
            offset.z = 0;
            var distance = offset.magnitude;
            float scale = distance / 5.0f;
            var sourcePosInThisSpace = this.transform.InverseTransformDirection(offset);
            particleEffect.transform.localScale = sourcePosInThisSpace.x > 0 ? scale * new Vector3(1, 1, 1) : scale * new Vector3(-1, 1, 1);
        }
        else
        {
            this.particleEffect.SetEmissionEnabled(false);
        }
    }
};
