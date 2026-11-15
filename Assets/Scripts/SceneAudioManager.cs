using UnityEngine;
using UnityEngine.UI;

public class SceneAudioManager : MonoBehaviour
{
    [System.Serializable]
    private class ButtonSoundOverride
    {
        public Button button;
        public AudioClip clickSound;
        public AudioClip hoverSound;
    }

    [Header("Music for this scene")]
    [SerializeField] private AudioClip backgroundMusic;
    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.65f;

    [Header("Buttons in this scene")]
    [SerializeField] private AudioClip buttonClickSound;
    [SerializeField] private AudioClip buttonHoverSound;
    [SerializeField, Range(0f, 1f)] private float buttonSoundVolume = 0.8f;
    [SerializeField] private ButtonSoundOverride[] buttonOverrides = new ButtonSoundOverride[0];

    private AudioSource musicSource;
    private AudioSource buttonSource;

    private void Awake()
    {
        musicSource = CreateSource("Scene Music");
        musicSource.loop = true;
        musicSource.volume = musicVolume;

        buttonSource = CreateSource("Button Sounds");
    }

    private void Start()
    {
        if (backgroundMusic != null)
        {
            musicSource.clip = backgroundMusic;
            musicSource.Play();
        }

        // Start runs after every scene object's Awake, including the Home settings menu.
        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Include);
        foreach (Button button in buttons)
        {
            if (button.GetComponent<ButtonAudioFeedback>() != null) continue;

            AudioClip clickOverride = null;
            AudioClip hoverOverride = null;
            if (buttonOverrides != null)
            {
                foreach (ButtonSoundOverride entry in buttonOverrides)
                {
                    if (entry == null || entry.button != button) continue;
                    clickOverride = entry.clickSound;
                    hoverOverride = entry.hoverSound;
                    break;
                }
            }

            button.gameObject.AddComponent<ButtonAudioFeedback>()
                .Initialize(this, button, clickOverride, hoverOverride);
        }
    }

    private AudioSource CreateSource(string name)
    {
        GameObject child = new GameObject(name, typeof(AudioSource));
        child.transform.SetParent(transform, false);
        AudioSource source = child.GetComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        return source;
    }

    public void PlayButtonClick(AudioClip clipOverride)
    {
        PlayButtonSound(clipOverride != null ? clipOverride : buttonClickSound);
    }

    public void PlayButtonHover(AudioClip clipOverride)
    {
        PlayButtonSound(clipOverride != null ? clipOverride : buttonHoverSound);
    }

    private void PlayButtonSound(AudioClip clip)
    {
        if (clip != null)
            buttonSource.PlayOneShot(clip, buttonSoundVolume * GameAudioSettings.SoundEffectsVolume);
    }
}
