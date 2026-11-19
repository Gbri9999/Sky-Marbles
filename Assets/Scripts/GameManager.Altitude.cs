using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class GameManager
{
    private const int AltitudeMarkerStep = 100;
    private const int AltitudeMilestoneStep = 500;

    private readonly Queue<int> pendingAltitudeMilestones = new Queue<int>();
    private readonly Queue<AltitudeMarker> altitudeMarkers = new Queue<AltitudeMarker>();
    private float startingAltitude;
    private float highestAltitude;
    private int nextAltitudeMarker = 1;
    private int nextAltitudeMilestone = 1;
    private Transform altitudeMarkerRoot;
    private RectTransform altitudeNoticeRoot;
    private CanvasGroup altitudeNoticeGroup;
    private TMP_Text altitudeNoticeNumber;
    private TMP_Text altitudeNoticeCaption;
    private Coroutine altitudeNoticeAnimation;
    private Sprite altitudePanelSprite;

    private struct AltitudeMarker
    {
        public GameObject gameObject;
        public float height;
    }

    private void InitializeAltitudeMilestones()
    {
        startingAltitude = player.position.y;
        highestAltitude = startingAltitude;

        GameObject markerObject = new GameObject("Altitude Markers");
        altitudeMarkerRoot = markerObject.transform;
        altitudeMarkerRoot.SetParent(platformContainer.transform.parent, false);

        CreateAltitudePanelSprite();
        CreateAltitudeNotice();
    }

    private void UpdateAltitudeMilestones()
    {
        highestAltitude = Mathf.Max(highestAltitude, player.position.y);
        float climbed = highestAltitude - startingAltitude;

        // Keep the next marker ready before it enters the camera view.
        while (nextAltitudeMarker * AltitudeMarkerStep <= climbed + 25f)
        {
            int meters = nextAltitudeMarker * AltitudeMarkerStep;
            CreateAltitudeMarker(meters);
            nextAltitudeMarker++;
        }

        while (altitudeMarkers.Count > 0 && altitudeMarkers.Peek().height < highestAltitude - 20f)
            Destroy(altitudeMarkers.Dequeue().gameObject);

        while (nextAltitudeMilestone * AltitudeMilestoneStep <= climbed)
        {
            pendingAltitudeMilestones.Enqueue(nextAltitudeMilestone * AltitudeMilestoneStep);
            nextAltitudeMilestone++;
        }

        if (altitudeNoticeAnimation == null && pendingAltitudeMilestones.Count > 0)
            altitudeNoticeAnimation = StartCoroutine(PlayAltitudeNotices());
    }

    private void CreateAltitudeMarker(int meters)
    {
        bool milestone = meters % AltitudeMilestoneStep == 0;
        GameObject markerObject = new GameObject($"{meters} m Marker", typeof(RectTransform), typeof(Canvas));
        RectTransform rect = markerObject.GetComponent<RectTransform>();
        rect.SetParent(altitudeMarkerRoot, false);
        rect.position = new Vector3(0f, startingAltitude + meters, 0f);
        rect.localScale = Vector3.one * 0.01f;
        rect.sizeDelta = new Vector2(300f, 52f);

        Canvas canvas = markerObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        canvas.sortingOrder = -8;

        Color line = milestone
            ? new Color(1f, 0.76f, 0.36f, 0.78f)
            : new Color(0.65f, 0.84f, 1f, 0.48f);
        CreateAltitudeImage(rect, "Left Rule", new Vector2(-98f, 0f), new Vector2(62f, 2f), line);
        CreateAltitudeImage(rect, "Right Rule", new Vector2(98f, 0f), new Vector2(62f, 2f), line);
        CreateAltitudeImage(rect, "Left Dot", new Vector2(-135f, 0f), new Vector2(6f, 6f), line);
        CreateAltitudeImage(rect, "Right Dot", new Vector2(135f, 0f), new Vector2(6f, 6f), line);

        TMP_Text label = CreateAltitudeText(rect, "Meters", new Vector2(0f, 1f),
            new Vector2(120f, 38f), milestone ? 27f : 23f,
            milestone ? new Color(1f, 0.88f, 0.57f) : new Color(0.82f, 0.91f, 1f));
        label.text = $"{meters} m";

        altitudeMarkers.Enqueue(new AltitudeMarker
        {
            gameObject = markerObject,
            height = startingAltitude + meters
        });
    }

    private void CreateAltitudeNotice()
    {
        GameObject notice = new GameObject("Altitude Milestone Notice", typeof(RectTransform), typeof(CanvasGroup));
        altitudeNoticeRoot = notice.GetComponent<RectTransform>();
        altitudeNoticeRoot.SetParent(warningPanel.transform.parent, false);
        altitudeNoticeRoot.anchorMin = new Vector2(0.5f, 1f);
        altitudeNoticeRoot.anchorMax = new Vector2(0.5f, 1f);
        altitudeNoticeRoot.pivot = new Vector2(0.5f, 1f);
        altitudeNoticeRoot.anchoredPosition = new Vector2(0f, -112f);
        altitudeNoticeRoot.sizeDelta = new Vector2(460f, 125f);

        altitudeNoticeGroup = notice.GetComponent<CanvasGroup>();
        altitudeNoticeGroup.alpha = 0f;
        altitudeNoticeGroup.interactable = false;
        altitudeNoticeGroup.blocksRaycasts = false;

        CreateAltitudeImage(altitudeNoticeRoot, "Soft Glow", Vector2.zero,
            new Vector2(466f, 131f), new Color(0.33f, 0.7f, 1f, 0.25f), true);
        CreateAltitudeImage(altitudeNoticeRoot, "Golden Border", Vector2.zero,
            new Vector2(450f, 115f), new Color(1f, 0.75f, 0.36f, 0.96f), true);
        CreateAltitudeImage(altitudeNoticeRoot, "Night Panel", Vector2.zero,
            new Vector2(444f, 109f), new Color(0.055f, 0.09f, 0.2f, 0.97f), true);
        CreateAltitudeImage(altitudeNoticeRoot, "Left Accent", new Vector2(-210f, 0f),
            new Vector2(4f, 72f), new Color(0.43f, 0.9f, 1f));
        CreateAltitudeImage(altitudeNoticeRoot, "Right Accent", new Vector2(210f, 0f),
            new Vector2(4f, 72f), new Color(0.43f, 0.9f, 1f));

        TMP_Text heading = CreateAltitudeText(altitudeNoticeRoot, "Heading",
            new Vector2(0f, 31f), new Vector2(380f, 25f), 15f,
            new Color(0.57f, 0.88f, 1f));
        heading.text = "ALTITUDE MILESTONE";
        altitudeNoticeNumber = CreateAltitudeText(altitudeNoticeRoot, "Distance",
            new Vector2(0f, 0f), new Vector2(400f, 44f), 34f,
            new Color(1f, 0.89f, 0.56f));
        altitudeNoticeCaption = CreateAltitudeText(altitudeNoticeRoot, "Caption",
            new Vector2(0f, -35f), new Vector2(400f, 24f), 14f,
            new Color(0.89f, 0.94f, 1f));
    }

    private Image CreateAltitudeImage(RectTransform parent, string name, Vector2 position,
        Vector2 size, Color color, bool rounded = false)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Image image = gameObject.GetComponent<Image>();
        image.raycastTarget = false;
        image.color = color;
        if (rounded)
        {
            image.sprite = altitudePanelSprite;
            image.type = Image.Type.Sliced;
        }
        return image;
    }

    private TMP_Text CreateAltitudeText(RectTransform parent, string name, Vector2 position,
        Vector2 size, float fontSize, Color color)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        TMP_Text text = gameObject.GetComponent<TextMeshProUGUI>();
        text.font = warningText.font;
        text.fontSize = fontSize;
        text.fontStyle = FontStyles.Bold;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = true;
        text.fontSizeMin = fontSize * 0.7f;
        text.fontSizeMax = fontSize;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        return text;
    }

    private IEnumerator PlayAltitudeNotices()
    {
        while (pendingAltitudeMilestones.Count > 0 && !isGameOver)
        {
            int meters = pendingAltitudeMilestones.Dequeue();
            altitudeNoticeNumber.text = $"{meters} METERS";
            altitudeNoticeCaption.text = $"YOU REACHED {meters} METERS";

            float elapsed = 0f;
            while (elapsed < 0.42f)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / 0.42f);
                float ease = 1f - Mathf.Pow(1f - progress, 3f);
                altitudeNoticeGroup.alpha = ease;
                altitudeNoticeRoot.localScale = Vector3.one * Mathf.Lerp(0.82f, 1.03f, ease);
                altitudeNoticeRoot.anchoredPosition = new Vector2(0f, Mathf.Lerp(-92f, -112f, ease));
                yield return null;
            }

            altitudeNoticeRoot.localScale = Vector3.one;
            altitudeNoticeRoot.anchoredPosition = new Vector2(0f, -112f);
            yield return new WaitForSecondsRealtime(2.3f);

            elapsed = 0f;
            while (elapsed < 0.5f)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / 0.5f);
                altitudeNoticeGroup.alpha = 1f - progress * progress;
                yield return null;
            }
            altitudeNoticeGroup.alpha = 0f;
        }
        altitudeNoticeAnimation = null;
    }

    private void HideAltitudeMilestone()
    {
        pendingAltitudeMilestones.Clear();
        altitudeNoticeAnimation = null;
        if (altitudeNoticeGroup != null) altitudeNoticeGroup.alpha = 0f;
    }

    private void CreateAltitudePanelSprite()
    {
        const int size = 64;
        const float corner = 14f;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color32[] pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(Mathf.Abs(x - 31.5f) - (32f - corner), 0f);
                float dy = Mathf.Max(Mathf.Abs(y - 31.5f) - (32f - corner), 0f);
                float alpha = Mathf.Clamp01(corner + 0.5f - Mathf.Sqrt(dx * dx + dy * dy));
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(255f * alpha));
            }
        }
        texture.SetPixels32(pixels);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.Apply(false, true);
        altitudePanelSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
            new Vector4(16f, 16f, 16f, 16f));
    }

    private void DisposeAltitudeMilestones()
    {
        if (altitudePanelSprite != null)
        {
            Destroy(altitudePanelSprite.texture);
            Destroy(altitudePanelSprite);
        }
        if (altitudeNoticeRoot != null) Destroy(altitudeNoticeRoot.gameObject);
        if (altitudeMarkerRoot != null) Destroy(altitudeMarkerRoot.gameObject);
    }
}
