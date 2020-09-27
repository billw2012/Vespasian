using UnityEngine;

public class CameraShakeEffect : MonoBehaviour
{
    // How long it takes camera to animate from one place to another
    public float shakeCycleDuration = 0.05f;

    // Camera shake effect
    int shakeNCycles = 0;
    Vector3 shakeTargetOffset;
    Vector3 shakePrevOffset;
    // Progress of one cycle of shake
    float shakeCycleProgress = 0;
    // Progress of whole shake series
    float shakeSeriesProgess = 0;
    // Total duration of this shake sequence
    float shakeSeriesDuration = 0;

    public void StartShake(float duration)
    {
        this.shakeNCycles = Mathf.CeilToInt(duration / this.shakeCycleDuration);
        this.shakeSeriesProgess = 0;
        this.shakeCycleProgress = 0;
        this.shakeSeriesDuration = this.shakeNCycles * this.shakeCycleDuration;
    }

    void LateUpdate()
    {
        // Increase progress to next point
        this.shakeCycleProgress = Mathf.Clamp(this.shakeCycleProgress + Time.deltaTime / this.shakeCycleDuration, 0, 1);
        this.shakeSeriesProgess = Mathf.Clamp(this.shakeSeriesProgess + Time.deltaTime / this.shakeSeriesDuration, 0, 1);

        if (this.shakeCycleProgress >= 1.0f && this.shakeNCycles > 0)
        {
            this.shakeNCycles--;
            this.shakePrevOffset = this.shakeTargetOffset;
            this.shakeTargetOffset = this.shakeNCycles == 0 ? Vector3.zero : Random.insideUnitSphere;
            this.shakeCycleProgress = 0;
        }

        // Update position
        float shakeScale = 1.0f;
        float shakeAmplitude = shakeScale * ShakeAmplitude(this.shakeSeriesProgess);
        var targetPrevDifference = this.shakeTargetOffset - this.shakePrevOffset;

        // This presumes that position is being set every frame by another controller...
        this.transform.position += (shakeAmplitude * (this.shakePrevOffset + CosTransition(this.shakeCycleProgress) * targetPrevDifference)).xy0();

        //Debug.Log($"ShakeNCycles: {this.shakeNCycles}, shakeProgress: {this.shakeCycleProgress}, prev offset: {this.shakePrevOffset}, targetOffset: {this.shakeTargetOffset}, currentOffset: {this.shakeCurrentOffset}");
    }

    // Cos-like transition function, take input 0..1, returns value 0..1
    static float CosTransition(float valueNormalized)
    {
        var valueClamped = Mathf.Clamp(valueNormalized, 0, 1);
        return (0.5f - 0.5f * Mathf.Cos(valueClamped * Mathf.PI));
    }

    // Returns decaying value 1..0, input is 0..1
    static float ShakeAmplitude(float timeNormalized)
    {
        //float decayValue = Mathf.Abs(Mathf.Pow(timeNormalized - 1.0f, 2.0f));
        //return Mathf.Clamp(0.8f*decayValue + 0.1f, 0, 1);
        return Mathf.Clamp(Mathf.Pow(20.0f, -timeNormalized), 0, 1);
    }
}
