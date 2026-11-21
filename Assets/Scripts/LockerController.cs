using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LockerController : MonoBehaviour
{
    [Serializable]
    public class MarbleView
    {
        public string id;
        public CanvasGroup card;
        public Button button;
        public TMP_Text status;
        public GameObject slotPreview;
    }

    [Serializable]
    public class AmuletView
    {
        public TMP_Text nameLabel;
        public Image magnetIcon;
        public TMP_Text jumpIcon;
        public Button previous;
        public Button next;
    }

    [SerializeField] private MarbleView[] marbles;
    [SerializeField] private AmuletView[] amuletSlots;
    [SerializeField] private TMP_Text selectedMarbleLabel;
    [SerializeField] private Sprite patrickTotemIcon;
    [SerializeField] private Sprite bravyPortrait;
    [SerializeField] private Sprite voltyPortrait;
    [SerializeField] private Sprite marineePortrait;
    [SerializeField] private Sprite cheeseIcon;
    [SerializeField] private Sprite cyclopsIcon;
    [SerializeField] private Sprite dashIcon;
    [SerializeField] private Sprite shadowIcon;
    [SerializeField] private Sprite aspiratorIcon;

    private Sprite magnetIconSprite;
    private Image[] amuletCheeseShadows;
    private string[] animatedAmuletIds = new string[2];
    private RectTransform carouselViewport;
    private RectTransform[] marbleRects;
    private Canvas rootCanvas;
    private float carouselPosition;
    private float carouselTarget;
    private float carouselVelocity;
    private bool carouselDragging;
    private const float CarouselSpacing = 105f;

    private static readonly string[] SlotKeys = { StoreProgress.AmuletSlot1, StoreProgress.AmuletSlot2 };

    private void Awake()
    {
        magnetIconSprite = amuletSlots[0].magnetIcon.sprite;
        MarbleView template = marbles[2];
        List<MarbleView> views = new List<MarbleView>(marbles);
        views.Add(CreateMarbleView(template, StoreProgress.Bravy, bravyPortrait));
        views.Add(CreateMarbleView(template, StoreProgress.Volty, voltyPortrait));
        views.Add(CreateMarbleView(template, StoreProgress.Marinee, marineePortrait));
        marbles = views.ToArray();
        CreateMarbleCarousel();
        amuletCheeseShadows = new Image[amuletSlots.Length];
        for (int i = 0; i < amuletSlots.Length; i++)
            amuletCheeseShadows[i] = CreateCheeseShadow(amuletSlots[i].magnetIcon);
    }

    private MarbleView CreateMarbleView(MarbleView template, string id, Sprite portraitSprite)
    {
        GameObject card = Instantiate(template.card.gameObject, template.card.transform.parent);
        card.name = id + " Card";
        CanvasGroup group = card.GetComponent<CanvasGroup>();
        Button button = card.GetComponent<Button>();
        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener(() => EquipMarble(id));

        Image portrait = card.transform.Find("Portrait").GetComponent<Image>();
        Animator portraitAnimator = portrait.GetComponent<Animator>();
        if (portraitAnimator != null) portraitAnimator.enabled = false;
        portrait.sprite = portraitSprite;
        UiSpriteAnimation.Attach(portrait, portraitSprite);
        card.GetComponent<MarbleDragSource>().Configure(id, this, portrait);

        GameObject preview = Instantiate(template.slotPreview, template.slotPreview.transform.parent);
        preview.name = "Equipped " + id;
        Animator previewAnimator = preview.GetComponent<Animator>();
        if (previewAnimator != null) previewAnimator.enabled = false;
        preview.GetComponent<Image>().sprite = portraitSprite;
        UiSpriteAnimation.Attach(preview.GetComponent<Image>(), portraitSprite);
        preview.SetActive(false);

        return new MarbleView
        {
            id = id,
            card = group,
            button = button,
            status = card.transform.Find("Status").GetComponent<TMP_Text>(),
            slotPreview = preview
        };
    }

    private void OnEnable()
    {
        CleanInvalidEquipment();
        Refresh();
    }

    private void LateUpdate()
    {
        if (carouselViewport == null) return;
        if (!carouselDragging)
            carouselPosition = Mathf.SmoothDamp(carouselPosition, carouselTarget,
                ref carouselVelocity, 0.14f, Mathf.Infinity, Time.unscaledDeltaTime);
        LayoutMarbleCarousel();
    }

    private void CreateMarbleCarousel()
    {
        RectTransform canvas = (RectTransform)marbles[0].card.transform.parent;
        rootCanvas = canvas.GetComponent<Canvas>();
        GameObject viewport = new GameObject("Marble Carousel Viewport",
            typeof(RectTransform), typeof(Image), typeof(RectMask2D), typeof(MarbleCarouselInput));
        carouselViewport = viewport.GetComponent<RectTransform>();
        carouselViewport.SetParent(canvas, false);
        carouselViewport.anchorMin = carouselViewport.anchorMax = new Vector2(0.5f, 0.5f);
        carouselViewport.anchoredPosition = new Vector2(-181f, 101f);
        carouselViewport.sizeDelta = new Vector2(320f, 104f);
        Image inputSurface = viewport.GetComponent<Image>();
        inputSurface.color = Color.clear;
        inputSurface.raycastTarget = true;
        viewport.GetComponent<MarbleCarouselInput>().Configure(this);

        marbleRects = new RectTransform[marbles.Length];
        for (int i = 0; i < marbles.Length; i++)
        {
            RectTransform card = marbles[i].card.GetComponent<RectTransform>();
            card.SetParent(carouselViewport, false);
            card.anchorMin = card.anchorMax = new Vector2(0.5f, 0.5f);
            card.pivot = new Vector2(0.5f, 0.5f);
            marbleRects[i] = card;
        }

        string selected = PlayerPrefs.GetString(StoreProgress.SelectedCharacter, StoreProgress.Bluey);
        int selectedIndex = Array.FindIndex(marbles, marble => marble.id == selected);
        if (selectedIndex < 0 || !IsMarbleOwned(marbles[selectedIndex].id)) selectedIndex = 0;
        carouselPosition = carouselTarget = selectedIndex;
        CreateCarouselArrow(canvas, "Marble Previous", "<", -362f, -1);
        CreateCarouselArrow(canvas, "Marble Next", ">", 0f, 1);
        TMP_Text heading = canvas.Find("Marbles Heading")?.GetComponent<TMP_Text>();
        if (heading != null) heading.text = "MARBLES  /  SWIPE OR TAP";
        LayoutMarbleCarousel();
    }

    private void CreateCarouselArrow(RectTransform parent, string name, string glyph,
        float x, int direction)
    {
        GameObject arrow = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform rect = arrow.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(x, 101f);
        rect.sizeDelta = new Vector2(28f, 52f);
        Image background = arrow.GetComponent<Image>();
        background.color = new Color(0.11f, 0.17f, 0.31f, 0.95f);
        Button button = arrow.GetComponent<Button>();
        button.targetGraphic = background;
        button.onClick.AddListener(() => MoveMarbleCarousel(direction));

        GameObject labelObject = new GameObject("Arrow", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.SetParent(rect, false);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
        TMP_Text label = labelObject.GetComponent<TextMeshProUGUI>();
        label.font = selectedMarbleLabel.font;
        label.fontSize = 27f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(0.85f, 0.96f, 1f);
        label.raycastTarget = false;
        label.text = glyph;
    }

    private Image CreateCheeseShadow(Image icon)
    {
        RectTransform original = icon.rectTransform;
        GameObject shadowObject = new GameObject("Cheese Shadow", typeof(RectTransform), typeof(Image));
        RectTransform rect = shadowObject.GetComponent<RectTransform>();
        rect.SetParent(original.parent, false);
        rect.SetSiblingIndex(original.GetSiblingIndex());
        rect.anchorMin = original.anchorMin;
        rect.anchorMax = original.anchorMax;
        rect.pivot = original.pivot;
        rect.anchoredPosition = original.anchoredPosition + new Vector2(3f, -3f);
        rect.sizeDelta = original.sizeDelta + new Vector2(6f, 6f);
        Image shadow = shadowObject.GetComponent<Image>();
        shadow.sprite = cheeseIcon;
        shadow.color = new Color(0.015f, 0.025f, 0.075f, 0.9f);
        shadow.preserveAspect = true;
        shadow.raycastTarget = false;
        shadowObject.SetActive(false);
        return shadow;
    }

    private void LayoutMarbleCarousel()
    {
        if (marbleRects == null) return;
        for (int i = 0; i < marbles.Length; i++)
        {
            float distance = Mathf.Repeat(i - carouselPosition + marbles.Length * 0.5f,
                marbles.Length) - marbles.Length * 0.5f;
            float magnitude = Mathf.Abs(distance);
            marbleRects[i].anchoredPosition = new Vector2(distance * CarouselSpacing,
                -4f * Mathf.Min(magnitude, 1f));
            marbleRects[i].localScale = Vector3.one * Mathf.Lerp(1f, 0.86f,
                Mathf.Min(magnitude, 1f));
            marbles[i].card.alpha = (IsMarbleOwned(marbles[i].id) ? 1f : 0.42f) *
                                    Mathf.Clamp01(2f - magnitude);
            marbles[i].card.blocksRaycasts = magnitude < 1.6f;
        }
    }

    public void BeginMarbleCarouselDrag()
    {
        carouselDragging = true;
        carouselVelocity = 0f;
    }

    public void DragMarbleCarousel(float screenDeltaX)
    {
        if (!carouselDragging) BeginMarbleCarouselDrag();
        float scale = rootCanvas != null ? Mathf.Max(0.01f, rootCanvas.scaleFactor) : 1f;
        carouselPosition -= screenDeltaX / (scale * CarouselSpacing);
        carouselTarget = carouselPosition;
        LayoutMarbleCarousel();
    }

    public void EndMarbleCarouselDrag()
    {
        carouselDragging = false;
        carouselTarget = Mathf.Round(carouselPosition);
    }

    public void MoveMarbleCarousel(int direction)
    {
        carouselDragging = false;
        carouselTarget = Mathf.Round(carouselTarget) + direction;
    }

    public void ScrollMarbleCarousel(float delta)
    {
        if (Mathf.Abs(delta) > 0.01f) MoveMarbleCarousel(delta > 0f ? -1 : 1);
    }

    public bool IsMarbleOwned(string id)
    {
        return StoreProgress.IsOwned(id);
    }

    public void EquipMarble(string id)
    {
        if (!IsMarbleOwned(id)) return;

        PlayerPrefs.SetString(StoreProgress.SelectedCharacter, id);
        PlayerPrefs.Save();
        int selectedIndex = Array.FindIndex(marbles, marble => marble.id == id);
        if (selectedIndex >= 0 && !carouselDragging)
        {
            float count = marbles.Length;
            carouselTarget += Mathf.Repeat(selectedIndex - carouselTarget + count * 0.5f, count) - count * 0.5f;
        }
        Refresh();
    }

    public void PreviousAmulet1() => CycleAmulet(0, -1);
    public void NextAmulet1() => CycleAmulet(0, 1);
    public void PreviousAmulet2() => CycleAmulet(1, -1);
    public void NextAmulet2() => CycleAmulet(1, 1);

    private void CycleAmulet(int slot, int direction)
    {
        List<string> options = AvailableAmulets(slot);
        int current = options.IndexOf(PlayerPrefs.GetString(SlotKeys[slot], ""));
        if (current < 0) current = 0;
        int next = (current + direction + options.Count) % options.Count;
        string chosen = options[next];

        PlayerPrefs.SetString(SlotKeys[slot], chosen);
        PlayerPrefs.Save();
        Refresh();
    }

    private List<string> AvailableAmulets(int slot)
    {
        List<string> options = new List<string> { "" };
        string other = PlayerPrefs.GetString(SlotKeys[1 - slot], "");
        if (StoreProgress.IsOwned(StoreProgress.DoubleJump) && other != StoreProgress.DoubleJump)
            options.Add(StoreProgress.DoubleJump);
        if (StoreProgress.IsOwned(StoreProgress.Magnet) && other != StoreProgress.Magnet)
            options.Add(StoreProgress.Magnet);
        if (StoreProgress.IsOwned(StoreProgress.Aspirator) && other != StoreProgress.Aspirator)
            options.Add(StoreProgress.Aspirator);
        if (StoreProgress.IsOwned(StoreProgress.PatrickTotem) && other != StoreProgress.PatrickTotem)
            options.Add(StoreProgress.PatrickTotem);
        if (StoreProgress.IsOwned(StoreProgress.Cheese) && other != StoreProgress.Cheese)
            options.Add(StoreProgress.Cheese);
        if (StoreProgress.IsOwned(StoreProgress.Cyclops) && other != StoreProgress.Cyclops)
            options.Add(StoreProgress.Cyclops);
        if (StoreProgress.IsOwned(StoreProgress.Dash) && other != StoreProgress.Dash)
            options.Add(StoreProgress.Dash);
        if (StoreProgress.IsOwned(StoreProgress.Shadow) && other != StoreProgress.Shadow)
            options.Add(StoreProgress.Shadow);
        return options;
    }

    private void CleanInvalidEquipment()
    {
        for (int i = 0; i < 2; i++)
        {
            string id = PlayerPrefs.GetString(SlotKeys[i], "");
            if (id != "" && !StoreProgress.IsOwned(id)) PlayerPrefs.SetString(SlotKeys[i], "");
        }

        string first = PlayerPrefs.GetString(SlotKeys[0], "");
        if (first != "" && first == PlayerPrefs.GetString(SlotKeys[1], ""))
            PlayerPrefs.SetString(SlotKeys[1], "");
        PlayerPrefs.Save();
    }

    private void Refresh()
    {
        string selected = PlayerPrefs.GetString(StoreProgress.SelectedCharacter, StoreProgress.Bluey);
        if (!IsMarbleOwned(selected)) selected = StoreProgress.Bluey;
        selectedMarbleLabel.text = selected == StoreProgress.Breeze ? "BREEZE" :
                                   selected == StoreProgress.Bouncy ? "BOUNCY" :
                                   selected == StoreProgress.Bravy ? "BRAVY" :
                                   selected == StoreProgress.Volty ? "VOLTY" :
                                   selected == StoreProgress.Marinee ? "MARINEE" : "BLUEY";

        foreach (MarbleView marble in marbles)
        {
            bool owned = IsMarbleOwned(marble.id);
            marble.card.alpha = owned ? 1f : 0.4f;
            marble.button.interactable = owned;
            marble.status.text = !owned ? "LOCKED" : selected == marble.id ? "EQUIPPED" : "DRAG OR TAP";
            marble.slotPreview.SetActive(selected == marble.id);
        }

        for (int i = 0; i < 2; i++)
        {
            List<string> options = AvailableAmulets(i);
            string id = PlayerPrefs.GetString(SlotKeys[i], "");
            AmuletView view = amuletSlots[i];
            view.nameLabel.text = id == StoreProgress.DoubleJump ? "DOUBLE JUMP" :
                                  id == StoreProgress.Magnet ? "MAGNET" :
                                  id == StoreProgress.Aspirator ? "ASPIRATOR" :
                                  id == StoreProgress.PatrickTotem ? "PATRICK TOTEM" :
                                  id == StoreProgress.Cheese ? "CHEESE" :
                                  id == StoreProgress.Cyclops ? "CYCLOPS" :
                                  id == StoreProgress.Dash ? "DASH" :
                                  id == StoreProgress.Shadow ? "SHADOW" : "EMPTY";
            view.jumpIcon.gameObject.SetActive(id == StoreProgress.DoubleJump);
            view.magnetIcon.sprite = id == StoreProgress.Aspirator ? aspiratorIcon :
                                     id == StoreProgress.PatrickTotem ? patrickTotemIcon :
                                     id == StoreProgress.Cheese ? cheeseIcon :
                                     id == StoreProgress.Cyclops ? cyclopsIcon :
                                     id == StoreProgress.Dash ? dashIcon :
                                     id == StoreProgress.Shadow ? shadowIcon : magnetIconSprite;
            if (animatedAmuletIds[i] != id)
            {
                UiSpriteAnimation oldAnimation = view.magnetIcon.GetComponent<UiSpriteAnimation>();
                if (oldAnimation != null) { oldAnimation.enabled = false; Destroy(oldAnimation); }
                if (id == StoreProgress.Cyclops || id == StoreProgress.Dash || id == StoreProgress.Shadow)
                    UiSpriteAnimation.Attach(view.magnetIcon, view.magnetIcon.sprite);
                animatedAmuletIds[i] = id;
            }
            view.magnetIcon.gameObject.SetActive(id == StoreProgress.Magnet ||
                                                 id == StoreProgress.Aspirator ||
                                                 id == StoreProgress.PatrickTotem ||
                                                 id == StoreProgress.Cheese ||
                                                 id == StoreProgress.Cyclops ||
                                                 id == StoreProgress.Dash ||
                                                 id == StoreProgress.Shadow);
            if (amuletCheeseShadows != null)
                amuletCheeseShadows[i].gameObject.SetActive(id == StoreProgress.Cheese);
            view.previous.interactable = options.Count > 1;
            view.next.interactable = options.Count > 1;
        }
        LayoutMarbleCarousel();
    }
}
