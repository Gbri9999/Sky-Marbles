using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public partial class GameManager
{
    private static bool introPending;
    private static float introVisualDuration;
    private static float introAudioDuration;
    private static int introFadeSteps;

    private GameObject introOverlay;

    public static void ConfigureNextSceneIntroFade(float duration, int steps)
    {
        introVisualDuration = Mathf.Max(0f, duration);
        introFadeSteps = Mathf.Max(2, steps);
        introPending = true;
    }

    public static void ConfigureNextSceneIntroAudioFade(float duration)
    {
        introAudioDuration = Mathf.Max(0f, duration);
        introPending = true;
    }

    private void OnEnable()
    {
        if (introPending) StartCoroutine(PlaySceneIntro());
    }

    private IEnumerator PlaySceneIntro()
    {
        float visualDuration = introVisualDuration;
        float audioDuration = introAudioDuration;
        int steps = introFadeSteps;
        introPending = false;
        introVisualDuration = 0f;
        introAudioDuration = 0f;

        Image veil = null;
        if (visualDuration > 0f)
        {
            introOverlay = new GameObject("Scene Intro Fade", typeof(Canvas), typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            Canvas canvas = introOverlay.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            GameObject imageObject = new GameObject("Black Veil", typeof(RectTransform), typeof(Image));
            RectTransform rect = imageObject.GetComponent<RectTransform>();
            rect.SetParent(introOverlay.transform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            veil = imageObject.GetComponent<Image>();
            veil.color = Color.black;
        }

        if (audioDuration > 0f) AudioListener.volume = 0f;

        float elapsed = 0f;
        float totalDuration = Mathf.Max(visualDuration, audioDuration);
        while (elapsed < totalDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            if (veil != null)
            {
                float progress = Mathf.Clamp01(elapsed / visualDuration);
                float stepped = Mathf.Floor(progress * steps) / steps;
                veil.color = new Color(0f, 0f, 0f, 1f - stepped);
            }

            if (audioDuration > 0f)
                AudioListener.volume = GameAudioSettings.MasterVolume * Mathf.Clamp01(elapsed / audioDuration);

            yield return null;
        }

        EndSceneIntro();
    }

    private void EndSceneIntro()
    {
        AudioListener.volume = GameAudioSettings.MasterVolume;
        if (introOverlay != null) Destroy(introOverlay);
        introOverlay = null;
    }

    private void OnDisable()
    {
        EndSceneIntro();
    }
}
