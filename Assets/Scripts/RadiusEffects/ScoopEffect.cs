using UnityEngine;

public class ScoopEffect : RadiusEffect
{
    protected override void Apply(RadiusEffectTarget target, float value, float heightRatio, Vector3 direction)
    {
        //Debug.Log($"Scoop effect: {value}  height ratio: {heightRatio}");
        var playerLogic = target.GetComponent<PlayerLogic>();
        if (playerLogic != null)
        {
            playerLogic.AddFuel(value);
            var particleEffect = playerLogic.scoopEffect;
            //var particleEffectTransform = playerLogic.scoopEffectTransform;
            if (value > 0)
            {
                particleEffect.SetEmissionEnabled(true);
                var offset = transform.position - target.transform.position;
                offset.z = 0;

                var distance = offset.magnitude;
                float scale = distance / 5.0f;

                // Set emission amount based on height ratio
                //particleEffect.SetEmissionRateOverTimeMultiplier(50.0f*Mathf.Clamp(0.3f + 1.0f*heightRatio, 0, 1));

                // Check if the source is on the left or right, flip particle effect if needed
                var thisPosInTargetSpace = target.transform.InverseTransformDirection(offset);
                particleEffect.transform.localScale = thisPosInTargetSpace.x > 0 ? scale * new Vector3(1, 1, 1) : scale * new Vector3(-1, 1, 1);

                // Rotate particle effect towards source
                //particleEffectTransform.LookAt(this.transform.position, Vector3.forward);
            }
            else
            {
                particleEffect.SetEmissionEnabled(false);
            }
        }
    }
};
