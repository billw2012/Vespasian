using UnityEngine;

public class FadeableAudio : MonoBehaviour
{
    private AudioSource audioSource;
    private float initialVolume;

    private float duration;
    private float startTime;
    private float startVolume;
    private float targetVolume;

    private void Awake()
    {
        this.audioSource = this.GetComponent<AudioSource>();
        this.initialVolume = this.audioSource.volume;
        this.audioSource.volume = 0;
    }

    //void Start()
    //{
    //    //this.audioSource.Play();
    //}

    public void FadeIn(float duration)
    {
        this.startTime = Time.time;
        this.startVolume = this.audioSource.volume;
        this.duration = duration * (this.initialVolume - this.audioSource.volume) / this.initialVolume;
        this.targetVolume = this.initialVolume;
    }

    public void FadeOut(float duration)
    {
        this.startTime = Time.time;
        this.startVolume = this.audioSource.volume;
        this.duration = duration * this.audioSource.volume / this.initialVolume;
        this.targetVolume = 0;
    }

    private void FixedUpdate()
    {
        this.audioSource.volume = Mathf.Lerp(this.startVolume, this.targetVolume, (Time.time - this.startTime) / this.duration);
        if (this.audioSource.volume > 0 && !this.audioSource.isPlaying)
        {
            this.audioSource.Play();
        }
        else if (this.audioSource.volume == 0 && this.audioSource.isPlaying)
        {
            this.audioSource.Stop();
        }
    }
}
