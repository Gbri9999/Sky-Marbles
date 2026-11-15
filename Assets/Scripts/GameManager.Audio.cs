using UnityEngine;

public partial class GameManager
{
    [Header("Gameplay sound effects")]
    [SerializeField] private AudioClip warningSound;
    [SerializeField] private AudioClip earthquakeSound;
    [SerializeField] private AudioClip laserImpactSound;
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private AudioClip deathSound;
    [SerializeField, Range(0f, 1f)] private float gameplaySoundVolume = 0.8f;

    private AudioSource gameplaySoundSource;
    private JumpSpeedLines jumpSpeedLines;

    private void Awake()
    {
        gameplaySoundSource = gameObject.AddComponent<AudioSource>();
        gameplaySoundSource.playOnAwake = false;
        gameplaySoundSource.spatialBlend = 0f;
    }

    private void PlayGameplaySound(AudioClip clip)
    {
        if (clip != null)
            gameplaySoundSource.PlayOneShot(clip, gameplaySoundVolume * GameAudioSettings.SoundEffectsVolume);
    }

    public void PlayJumpFeedback()
    {
        PlayGameplaySound(jumpSound);
        if (jumpSpeedLines == null && Camera.main != null)
            jumpSpeedLines = Camera.main.GetComponent<JumpSpeedLines>();
        if (jumpSpeedLines != null) jumpSpeedLines.Play();
    }

    public void PlayLaserImpactSound()
    {
        PlayGameplaySound(laserImpactSound);
    }
}
