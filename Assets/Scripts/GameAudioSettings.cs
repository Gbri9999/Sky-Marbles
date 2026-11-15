using UnityEngine;

public static class GameAudioSettings
{
    private const string MasterVolumeKey = "Settings.MasterVolume";
    private const string SoundEffectsVolumeKey = "Settings.SoundEffectsVolume";

    public static float MasterVolume => PlayerPrefs.GetFloat(MasterVolumeKey, 1f);
    public static float SoundEffectsVolume => PlayerPrefs.GetFloat(SoundEffectsVolumeKey, 1f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ApplySavedVolume()
    {
        AudioListener.volume = Mathf.Clamp01(MasterVolume);
    }

    public static void SetMasterVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(MasterVolumeKey, volume);
        AudioListener.volume = volume;
    }

    public static void SetSoundEffectsVolume(float volume)
    {
        PlayerPrefs.SetFloat(SoundEffectsVolumeKey, Mathf.Clamp01(volume));
    }

}
