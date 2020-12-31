using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class RadialDetectorVisualComponent : MonoBehaviour
{
    [SerializeField]
    private SimpleSpectrum spectrum;

    [SerializeField]
    private float sustainTime = 0.25f;  
        
    private float detectionTimeout;

    private void LateUpdate()
    {
        // We are parented to the player, but we don't want to rotate with them, so zero out global rotation here
        // (actually we need to rotate 90 degrees in x for the spectrum to align properly with the screen for some reason...)
        this.transform.rotation = Quaternion.Euler(90, 0, 0);
    }

    public async void UpdateDetections(IEnumerable<(Vector2 position, float value)> detections)
    {
        var samples = new float[this.spectrum.numSamples];
        var detectorPos = (Vector2)this.transform.position;

        float DirectionSample(Vector2 target)
        {
            return ((360f + Vector2.SignedAngle(Vector2.up, target - detectorPos)) / 360f) % 1f;
        }
        
        var detectionRadial = detections.Select(d =>
            (
                mean: samples.Length * DirectionSample(d.position),
                value: d.value
            )).ToList();
        for (int i = 0; i < samples.Length; i++)
        {
            foreach (var (mean, value) in detectionRadial)
            {
                samples[i] += MathX.NormalDistributionFixedHeight(i, mean, value) * value;
            }
        }
        // map detections to samples
        this.spectrum.spectrumInputData = samples;

        // Keep the values set for a small amount of time to allow them to peak
        await Awaiters.Seconds(this.sustainTime);

        // Zero them again
        this.spectrum.spectrumInputData = new float[this.spectrum.numSamples];
    }

    public async Task TestFireAsync()
    {
        
    }
}