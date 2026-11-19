
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    [SerializeField] private CanvasGroup fadeOverlay;
    [SerializeField, Min(0.01f)] private float fadeDuration = 0.38f;

    private bool changingScene;
    private Coroutine openingFade;

    private void Awake()
    {
        Time.timeScale = 1f;
        if (fadeOverlay == null) return;

        fadeOverlay.alpha = 1f;
        fadeOverlay.interactable = false;
        fadeOverlay.blocksRaycasts = true;
    }

    private void Start()
    {
        if (fadeOverlay != null) openingFade = StartCoroutine(FadeFromBlack());
    }

    public void ChangeScene(string sceneName)
    {
        if (changingScene) return;
        changingScene = true;
        if (openingFade != null) StopCoroutine(openingFade);
        StartCoroutine(FadeToScene(sceneName));
    }

    private IEnumerator FadeFromBlack()
    {
        fadeOverlay.transform.SetAsLastSibling();
        yield return Fade(1f, 0f);
        fadeOverlay.blocksRaycasts = false;
        openingFade = null;
    }

    private IEnumerator FadeToScene(string sceneName)
    {
        if (fadeOverlay != null)
        {
            fadeOverlay.transform.SetAsLastSibling();
            fadeOverlay.blocksRaycasts = true;
            yield return Fade(fadeOverlay.alpha, 1f);
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }

    private IEnumerator Fade(float from, float to)
    {
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            if (fadeOverlay.transform.GetSiblingIndex() != fadeOverlay.transform.parent.childCount - 1)
                fadeOverlay.transform.SetAsLastSibling();
            elapsed += Time.unscaledDeltaTime;
            fadeOverlay.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / fadeDuration));
            yield return null;
        }

        fadeOverlay.alpha = to;
    }
}
