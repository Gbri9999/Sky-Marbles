using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(Canvas))]
public class HomeSettingsMenu : MonoBehaviour
{
    private static readonly Color Ink = new Color(0.065f, 0.105f, 0.19f, 1f);
    private static readonly Color DeepInk = new Color(0.035f, 0.065f, 0.13f, 1f);
    private static readonly Color Cream = new Color(1f, 0.945f, 0.839f, 1f);
    private static readonly Color Cyan = new Color(0.725f, 0.863f, 0.89f, 1f);
    private static readonly Color Pink = new Color(0.953f, 0.659f, 0.769f, 1f);

    private TMP_FontAsset pixelFont;
    private GameObject settingsOverlay;
    private GameObject confirmationOverlay;
    private Button menuButton;
    private Button closeButton;
    private Button cancelButton;
    private TMP_Text statusText;

    private void Awake()
    {
        TMP_Text existingText = GetComponentInChildren<TMP_Text>(true);
        pixelFont = existingText != null ? existingText.font : TMP_Settings.defaultFontAsset;

        BuildMenuButton();
        BuildSettingsOverlay();
        BuildConfirmationOverlay();
        Transform blackFade = transform.Find("Black Fade");
        if (blackFade != null) blackFade.SetAsLastSibling();
        settingsOverlay.SetActive(false);
        confirmationOverlay.SetActive(false);
    }

    private void Update()
    {
#if ENABLE_INPUT_SYSTEM
        bool escapePressed = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
        bool escapePressed = Input.GetKeyDown(KeyCode.Escape);
#endif
        if (!escapePressed) return;

        if (confirmationOverlay.activeSelf) CancelDelete();
        else if (settingsOverlay.activeSelf) CloseSettings();
    }

    private void OnDisable()
    {
        PlayerPrefs.Save();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused) PlayerPrefs.Save();
    }

    private void BuildMenuButton()
    {
        RectTransform buttonRect = CreateRect("Settings Button", transform, new Vector2(0f, 1f),
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -20f), new Vector2(50f, 46f));
        Image background = AddImage(buttonRect, Ink);
        menuButton = AddButton(buttonRect, background, OpenSettings);

        CreateImage("Top Edge", buttonRect, Cyan, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f), Vector2.zero, new Vector2(50f, 3f));

        for (int i = 0; i < 3; i++)
        {
            CreateImage("Menu Bar " + (i + 1), buttonRect, Pink, Center,
                Center, Center, new Vector2(0f, 8f - i * 8f), new Vector2(22f, 3f));
        }
    }

    private void BuildSettingsOverlay()
    {
        RectTransform overlay = CreateRect("Settings Overlay", transform, Vector2.zero, Vector2.one,
            new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        settingsOverlay = overlay.gameObject;
        AddImage(overlay, new Color(0.015f, 0.03f, 0.08f, 0.84f));

        RectTransform border = CreatePanel(overlay, "Settings Panel", new Vector2(530f, 430f), Cyan);
        RectTransform card = CreateRect("Card", border, Vector2.zero, Vector2.one,
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-6f, -6f));
        AddImage(card, Ink);
        CreateImage("Header Stripe", card, Pink, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0f, -3f), new Vector2(0f, 5f));

        CreateText("Heading", card, "SETTINGS", 34f, Cream, TextAlignmentOptions.Center,
            Center, Center, Center, new Vector2(0f, 161f), new Vector2(420f, 50f));
        CreateImage("Title Accent", card, Pink, Center, Center, Center,
            new Vector2(0f, 128f), new Vector2(240f, 3f));

        closeButton = CreateTextButton(card, "Close", "X", new Vector2(1f, 1f),
            new Vector2(-29f, -27f), new Vector2(40f, 40f), DeepInk, Cyan, CloseSettings);

        CreateSliderRow(card, "MASTER VOLUME", 72f, GameAudioSettings.MasterVolume,
            GameAudioSettings.SetMasterVolume);
        CreateSliderRow(card, "SOUND EFFECTS", -21f, GameAudioSettings.SoundEffectsVolume,
            GameAudioSettings.SetSoundEffectsVolume);

        CreateTextButton(card, "Delete Save Data", "DELETE SAVE DATA", Center,
            new Vector2(0f, -128f), new Vector2(276f, 45f), DeepInk, Pink, ShowDeleteConfirmation);
        statusText = CreateText("Status", card, "COINS, ITEMS AND PROGRESS", 11f, Cyan,
            TextAlignmentOptions.Center, Center, Center, Center,
            new Vector2(0f, -176f), new Vector2(440f, 24f));

    }

    private void BuildConfirmationOverlay()
    {
        RectTransform overlay = CreateRect("Delete Confirmation", transform, Vector2.zero, Vector2.one,
            Center, Vector2.zero, Vector2.zero);
        confirmationOverlay = overlay.gameObject;
        AddImage(overlay, new Color(0.015f, 0.03f, 0.08f, 0.9f));

        RectTransform border = CreatePanel(overlay, "Confirmation Panel", new Vector2(450f, 240f), Pink);
        RectTransform card = CreateRect("Card", border, Vector2.zero, Vector2.one,
            Center, Vector2.zero, new Vector2(-6f, -6f));
        AddImage(card, Ink);
        CreateImage("Header Stripe", card, Cyan, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0f, -3f), new Vector2(0f, 5f));

        CreateText("Question", card, "ESTAS SEGURO?", 27f, Cream, TextAlignmentOptions.Center,
            Center, Center, Center, new Vector2(0f, 56f), new Vector2(410f, 50f));
        CreateText("Warning", card, "SE BORRARAN TODAS LAS PARTIDAS", 12f, Cyan,
            TextAlignmentOptions.Center, Center, Center, Center,
            new Vector2(0f, 11f), new Vector2(400f, 35f));

        CreateTextButton(card, "Yes", "Y", Center, new Vector2(-87f, -65f),
            new Vector2(135f, 48f), DeepInk, Pink, ConfirmDelete);
        cancelButton = CreateTextButton(card, "No", "N", Center, new Vector2(87f, -65f),
            new Vector2(135f, 48f), DeepInk, Cyan, CancelDelete);

    }

    private void CreateSliderRow(Transform parent, string label, float y, float initialValue,
        UnityEngine.Events.UnityAction<float> onChange)
    {
        CreateText(label + " Label", parent, label, 16f, Cream, TextAlignmentOptions.Left,
            Center, Center, Center, new Vector2(-25f, y + 21f), new Vector2(378f, 28f));

        RectTransform sliderRect = CreateRect(label + " Slider", parent, Center, Center, Center,
            new Vector2(-33f, y - 14f), new Vector2(360f, 30f));
        Slider slider = sliderRect.gameObject.AddComponent<Slider>();
        AddImage(sliderRect, new Color(0f, 0f, 0f, 0.001f));

        CreateImage("Track Border", sliderRect, Cyan, Center, Center, Center,
            Vector2.zero, new Vector2(360f, 16f));
        CreateImage("Track", sliderRect, DeepInk, Center, Center, Center,
            Vector2.zero, new Vector2(354f, 10f));

        RectTransform fillArea = CreateRect("Fill Area", sliderRect, Center, Center,
            Center, Vector2.zero, new Vector2(354f, 10f));
        RectTransform fill = CreateImage("Fill", fillArea, Pink, Vector2.zero,
            new Vector2(0f, 1f), new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);

        RectTransform handleArea = CreateRect("Handle Area", sliderRect, Center, Center,
            Center, Vector2.zero, new Vector2(354f, 26f));
        RectTransform handle = CreateImage("Handle", handleArea, Cream, Center, Center, Center,
            Vector2.zero, new Vector2(15f, 0f));

        TMP_Text valueText = CreateText(label + " Value", parent, "", 15f, Pink,
            TextAlignmentOptions.Right, Center, Center, Center,
            new Vector2(184f, y + 21f), new Vector2(55f, 27f));

        slider.fillRect = fill;
        slider.handleRect = handle;
        slider.targetGraphic = handle.GetComponent<Image>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.SetValueWithoutNotify(initialValue);
        valueText.text = Mathf.RoundToInt(initialValue * 100f) + "%";
        slider.onValueChanged.AddListener(value =>
        {
            onChange(value);
            valueText.text = Mathf.RoundToInt(value * 100f) + "%";
        });
    }

    private void OpenSettings()
    {
        menuButton.gameObject.SetActive(false);
        settingsOverlay.SetActive(true);
        Select(closeButton);
    }

    private void CloseSettings()
    {
        settingsOverlay.SetActive(false);
        menuButton.gameObject.SetActive(true);
        Select(menuButton);
        PlayerPrefs.Save();
    }

    private void ShowDeleteConfirmation()
    {
        confirmationOverlay.SetActive(true);
        Select(cancelButton);
    }

    private void CancelDelete()
    {
        confirmationOverlay.SetActive(false);
        Select(closeButton);
    }

    private void ConfirmDelete()
    {
        GameSaveSystem.ClearSaveData();
        confirmationOverlay.SetActive(false);
        statusText.text = "SAVE DATA DELETED";
        Select(closeButton);
    }

    private static void Select(Button button)
    {
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(button.gameObject);
    }

    private static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

    private static RectTransform CreateRect(string name, Transform parent, Vector2 min, Vector2 max,
        Vector2 pivot, Vector2 position, Vector2 size)
    {
        GameObject child = new GameObject(name, typeof(RectTransform));
        RectTransform rect = child.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private static Image AddImage(RectTransform rect, Color color)
    {
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        return image;
    }

    private static RectTransform CreateImage(string name, Transform parent, Color color, Vector2 min,
        Vector2 max, Vector2 pivot, Vector2 position, Vector2 size)
    {
        RectTransform rect = CreateRect(name, parent, min, max, pivot, position, size);
        Image image = AddImage(rect, color);
        image.raycastTarget = false;
        return rect;
    }

    private static RectTransform CreatePanel(Transform parent, string name, Vector2 size, Color borderColor)
    {
        RectTransform panel = CreateRect(name, parent, Center, Center, Center, Vector2.zero, size);
        AddImage(panel, borderColor);
        return panel;
    }

    private TMP_Text CreateText(string name, Transform parent, string value, float fontSize, Color color,
        TextAlignmentOptions alignment, Vector2 min, Vector2 max, Vector2 pivot, Vector2 position, Vector2 size)
    {
        RectTransform rect = CreateRect(name, parent, min, max, pivot, position, size);
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = pixelFont;
        text.text = value;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        return text;
    }

    private Button CreateTextButton(Transform parent, string name, string label, Vector2 anchor,
        Vector2 position, Vector2 size, Color backgroundColor, Color accentColor, UnityEngine.Events.UnityAction onClick)
    {
        RectTransform rect = CreateRect(name, parent, anchor, anchor, Center, position, size);
        Image background = AddImage(rect, backgroundColor);
        Button button = AddButton(rect, background, onClick);
        CreateImage("Accent", rect, accentColor, new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 3f));
        CreateText("Label", rect, label, label.Length > 4 ? 16f : 23f, Cream,
            TextAlignmentOptions.Center, Center, Center, Center, Vector2.zero, size - new Vector2(8f, 4f));
        return button;
    }

    private static Button AddButton(RectTransform rect, Image target, UnityEngine.Events.UnityAction onClick)
    {
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = target;
        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(0.82f, 1f, 1f, 1f);
        colors.pressedColor = new Color(0.7f, 0.8f, 0.9f, 1f);
        button.colors = colors;
        button.onClick.AddListener(onClick);
        return button;
    }
}
