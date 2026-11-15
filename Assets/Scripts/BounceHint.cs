using System.Collections;
using UnityEngine;

public class BounceHint : MonoBehaviour
{
    [SerializeField] private PlayerController player;
    [SerializeField] private CanvasGroup hint;
    [SerializeField, Min(0.01f)] private float fadeDuration = 0.35f;

    private bool fading;

    private void Update()
    {
        if (!fading && player != null && player.alreadyJumped)
        {
            fading = true;
            StartCoroutine(FadeOut());
        }
    }

    private IEnumerator FadeOut()
    {
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            hint.alpha = 1f - Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }

        hint.alpha = 0f;
        gameObject.SetActive(false);
    }
}
