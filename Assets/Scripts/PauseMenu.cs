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
        if (MobileControls.IsMobile)
        {
            Transform pauseButton = canvas.transform.Find("Pause Button");
            if (pauseButton != null)
            {
                RectTransform buttonRect = (RectTransform)pauseButton;
                buttonRect.sizeDelta = new Vector2(50f, 46f);
                TMP_Text label = pauseButton.GetComponentInChildren<TMP_Text>();
                if (label != null) label.gameObject.SetActive(false);
                for (int i = 0; i < 3; i++)
                {
                    GameObject line = new GameObject("Menu Bar", typeof(RectTransform), typeof(Image));
                    RectTransform rect = line.GetComponent<RectTransform>();
                    rect.SetParent(buttonRect, false);
                    rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                    rect.anchoredPosition = new Vector2(0f, 8f - i * 8f);
                    rect.sizeDelta = new Vector2(22f, 3f);
                    Image image = line.GetComponent<Image>();
                    image.color = new Color(0.953f, 0.659f, 0.769f);
                    image.raycastTarget = false;
                }
            }
        }
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
            Transform endRun = panel.Find("End Run");
            if (endRun != null)
            {
                endRun.name = "Resume";
                Button resume = endRun.GetComponent<Button>();
                resume.onClick = new Button.ButtonClickedEvent();
                resume.onClick.AddListener(Resume);
                endRun.GetComponentInChildren<TMP_Text>().text = "RESUME";
                PlaceButton(endRun, 30f);
            }
            PlaceButton(retry, -18f);
            PlaceButton(buttonObject.transform, -66f);
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

    public void Resume()
    {
        if (paused) TogglePause();
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
