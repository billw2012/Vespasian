using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class RadialDetectorVisualComponent : MonoBehaviour
{
    [SerializeField]
    private SimpleSpectrum spectrum = null;

    [SerializeField]
    private float sustainTime = 0.25f;  
    [SerializeField]
    private float distanceAngleRatio = 1f;
    [SerializeField]
    private float minDistanceAngle = 10f;
    [SerializeField]
    private float maxDistanceAngle = 180f;
    [SerializeField]
    private float valueHeightScaling = 1f;
        
    private void LateUpdate()
    {
        // We are parented to the player, but we don't want to rotate with them, so zero out global rotation here
        // (actually we need to rotate 90 degrees in x for the spectrum to align properly with the screen for some reason...)
        this.transform.rotation = Quaternion.Euler(90, 0, 0);
    }

    public async void UpdateDetections(IEnumerable<(Vector2 position, float value)> detections)
    {
        float[] samples = this.CalculateSamples(detections);

        // map detections to samples
        this.spectrum.spectrumInputData = samples;

        // Keep the values set for a small amount of time to allow them to peak
        await Awaiters.Seconds(this.sustainTime);

        // Zero them again
        this.spectrum.spectrumInputData = new float[this.spectrum.numSamples];
    }

    private float[] CalculateSamples(IEnumerable<(Vector2 position, float value)> detections)
    {
        var samples = new float[this.spectrum.numSamples];
        var detectorPos = (Vector2) this.transform.position;

        float CosDistributionFunction(float x, float width, float height) =>
            0.5f * height * (1 + Mathf.Cos(Mathf.PI * Mathf.Clamp(x / width, -1, 1)));

        foreach (var (target, value) in detections)
        {
            var targetVec = target - detectorPos;
            float targetAngle = Vector2.SignedAngle(Vector2.up, targetVec);
            float width = Mathf.Clamp(targetVec.magnitude * this.distanceAngleRatio, this.minDistanceAngle,
                this.maxDistanceAngle) / 360f;
            for (int i = 0; i < samples.Length; i++)
            {
                float sampleAngle = 360 * (i) / (float) samples.Length;
                float x = Mathf.DeltaAngle(sampleAngle, targetAngle) / 360f;
                samples[i] += CosDistributionFunction(x, width, Mathf.Clamp01(value) * this.valueHeightScaling);
            }
        }

        return samples;
    }

    public async Task TestFireAsync()
    {
        const float degrees = 720;
        const float testDistance = 20;
        const float testValue = 1;
        
        for (int i = 0; i < degrees; ++i)
        {
            var detectorPos = (Vector2) this.transform.position;
            var det = new[] {((Vector2)(Quaternion.Euler(0, 0, i) * Vector2.up) * testDistance + detectorPos, testValue)};
            this.spectrum.spectrumInputData = this.CalculateSamples(det);
            await Awaiters.Seconds(0.6f / degrees);
        }
        this.spectrum.spectrumInputData = new float[this.spectrum.numSamples];
    }
}