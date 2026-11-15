
using UnityEngine;
using System.Collections;

public class GameOver : MonoBehaviour
{
    public CanvasGroup gameOverPanel;
    public float fadeDuration = 2f;

    public GameManager manager;

    void Start()
    {
        gameOverPanel.alpha = 0f;
        gameOverPanel.interactable = false;
        gameOverPanel.blocksRaycasts = false;
    }

    public void ShowGameOver()
    {
        if (!manager.isGameOver)
        {
            manager.isGameOver = true;
            StartCoroutine(FadeIn());
        }
    }

    IEnumerator FadeIn()
    {
        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.unscaledDeltaTime;

            gameOverPanel.alpha = Mathf.Lerp(
                0f,
                1f,
                elapsedTime / fadeDuration
            );

            yield return null;
        }

        gameOverPanel.alpha = 1f;
        gameOverPanel.interactable = true;
        gameOverPanel.blocksRaycasts = true;
    }
}
