using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayMenu : MonoBehaviour
{
    [SerializeField] private Button playButton;
    [SerializeField] private CanvasGroup playPanel;
    [SerializeField] private Button storyModeButton;
    [SerializeField] private CanvasGroup worldsPanel;
    [SerializeField, Min(0.01f)] private float fadeDuration = 0.2f;

    private bool playUsed;
    private bool worldSelectionUsed;
    private Coroutine fadeRoutine;

    private void Awake()
    {
        playPanel.alpha = 0f;
        playPanel.interactable = false;
        playPanel.blocksRaycasts = false;

        worldsPanel.alpha = 0f;
        worldsPanel.interactable = false;
        worldsPanel.blocksRaycasts = false;

    }

    private void OnEnable()
    {
        if (playButton != null) playButton.onClick.AddListener(OpenPlayMenu);
        if (storyModeButton != null) storyModeButton.onClick.AddListener(OpenWorldSelection);
    }

    public void OpenPlayMenu()
    {
        if (playUsed) return;

        playUsed = true;
        playPanel.interactable = true;
        playPanel.blocksRaycasts = true;

        if (fadeRoutine != null) StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadePanel(playPanel,1f));
    }

    public void OpenWorldSelection()
    {
        if (worldSelectionUsed) return;

        worldSelectionUsed = true;
        worldsPanel.interactable = true;
        worldsPanel.blocksRaycasts = true;

        if (fadeRoutine != null) StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadePanel(worldsPanel,1f));
    }

    public void OpenPanel(CanvasGroup panel)
    {
        panel.gameObject.SetActive(true);
        panel.alpha = 0f;
        panel.interactable = true;
        panel.blocksRaycasts = true;

        if(fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadePanel(panel,1f));
    }

    public void ClosePanel(CanvasGroup panel)
    {
        panel.interactable = false;
        panel.blocksRaycasts = false;

        if(fadeRoutine != null) StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadePanel(panel, 0f));

        if(panel == playPanel) playUsed = false;
        if(panel == worldsPanel) worldSelectionUsed = false;
    }


    private static void PlaceButton(Transform target, float y)
    {
        if (target == null) return;
        RectTransform rect = (RectTransform)target;
        rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, y);
        rect.sizeDelta = new Vector2(rect.sizeDelta.x, 42f);
    }

    private IEnumerator FadePanel(CanvasGroup panel,float target)
    {
        float start = panel.alpha;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            panel.alpha = Mathf.Lerp(start, target, Mathf.Clamp01(elapsed / fadeDuration));
            yield return null;
        }

        panel.alpha = target;
        fadeRoutine = null;
    }

    private void OnDisable()
    {
        if (playButton != null) playButton.onClick.RemoveListener(OpenPlayMenu);
        if (storyModeButton != null) storyModeButton.onClick.RemoveListener(OpenWorldSelection);
    }
}
