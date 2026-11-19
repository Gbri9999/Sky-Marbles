using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private CanvasGroup pausePanel;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private SaveSystem saveSystem;
    [SerializeField, Min(0.01f)] private float fadeDuration = 0.2f;

    private bool paused;
    private Coroutine fadeRoutine;
    private HomeSettingsMenu settings;

    private void Awake()
    {
        pausePanel.alpha = 0f;
        pausePanel.interactable = false;
        pausePanel.blocksRaycasts = false;
        Canvas canvas = pausePanel.GetComponentInParent<Canvas>();
        settings = canvas.gameObject.AddComponent<HomeSettingsMenu>();
        settings.ConfigureForPause(saveSystem);

        RectTransform panel = pausePanel.GetComponent<RectTransform>();
        Transform retry = panel.Find("Retry");
        if (retry != null)
        {
            GameObject buttonObject = Instantiate(retry.gameObject, panel);
            buttonObject.name = "Settings";
            Button button = buttonObject.GetComponent<Button>();
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(settings.OpenSettings);
            buttonObject.GetComponentInChildren<TMP_Text>().text = "SETTINGS";
            PlaceButton(retry, 30f);
            PlaceButton(buttonObject.transform, -18f);
            PlaceButton(panel.Find("End Run"), -66f);
            PlaceButton(panel.Find("Main Menu"), -114f);
        }
    }

    private static void PlaceButton(Transform target, float y)
    {
        if (target == null) return;
        RectTransform rect = (RectTransform)target;
        rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, y);
        rect.sizeDelta = new Vector2(rect.sizeDelta.x, 42f);
    }

    private void Update()
    {
        if (Keyboard.current != null &&
            (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.qKey.wasPressedThisFrame))
        {
            if (settings != null && settings.IsOpen) settings.HandleBack();
            else TogglePause();
        }
    }

    public void TogglePause()
    {
        if (gameManager.isGameOver) return;

        paused = !paused;
        if (paused) saveSystem.SaveData();
        Time.timeScale = paused ? 0f : 1f;
        pausePanel.interactable = paused;
        pausePanel.blocksRaycasts = paused;

        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadePanel(paused ? 1f : 0f));
    }

    private IEnumerator FadePanel(float target)
    {
        float start = pausePanel.alpha;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            pausePanel.alpha = Mathf.Lerp(start, target, Mathf.Clamp01(elapsed / fadeDuration));
            yield return null;
        }

        pausePanel.alpha = target;
        fadeRoutine = null;
    }

    private void OnDisable()
    {
        if (paused) Time.timeScale = 1f;
    }
}
