using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StoreCatalog : MonoBehaviour
{
    [Serializable]
    public class Item
    {
        public string id;
        public int price;
        public Button button;
        public TMP_Text buttonLabel;
    }

    [SerializeField] private TMP_Text balanceLabel;
    [SerializeField] private TMP_Text messageLabel;
    [SerializeField] private Item[] items;
    [SerializeField] private Sprite bravyPortrait;
    [SerializeField] private Sprite voltyPortrait;
    [SerializeField] private Sprite marineePortrait;
    [SerializeField] private Sprite cheeseIcon;
    [SerializeField] private Sprite cyclopsIcon;
    [SerializeField] private Sprite dashIcon;
    [SerializeField] private Sprite shadowIcon;
    [SerializeField] private Sprite aspiratorIcon;

    private enum Category { General, Charms, Marbles }
    private Category selectedCategory;
    private readonly Image[] tabImages = new Image[3];
    private ScrollRect catalogScroll;

    private void Awake()
    {
        List<Item> catalog = new List<Item>(items);
        catalog.Add(CreateItem(items[2], StoreProgress.Bravy, 70, "Bravy", bravyPortrait,
            "STRONGER JUMP.\nBALANCED GRAVITY.", 3));
        catalog.Add(CreateItem(items[2], StoreProgress.Volty, 100, "Volty", voltyPortrait,
            "12% STRONGER JUMP, 2X SPEED.\nFLOATY FALL + EXTRA JUMP.", 4));
        catalog.Add(CreateItem(items[2], StoreProgress.Marinee, 200, "Marinee", marineePortrait,
            "LOW GRAVITY + TWO AIR JUMPS.\nMORE COINS AND BOUNCY FISH!", -1));
        catalog.Add(CreateItem(items[5], StoreProgress.Cheese, 90, "Cheese", cheeseIcon,
            "SUMMON A BOUNCY CHEESE BED.", -1));
        catalog.Add(CreateItem(items[5], StoreProgress.Cyclops, 120, "Cyclops", cyclopsIcon,
            "FOLLOWING COMPANION.\nBOUNCE ON IT AGAIN AND AGAIN.", -1));
        catalog.Add(CreateItem(items[5], StoreProgress.Dash, 100, "Dash", dashIcon,
            "AIM WITH WASD OR ARROWS.\nDASH AND REBOUND.", -1));
        catalog.Add(CreateItem(items[4], StoreProgress.Aspirator, 190, "Aspirator", aspiratorIcon,
            "PULL COINS FROM ACROSS THE MAP.\nEXTRA VERTICAL RANGE.", -1));
        catalog.Add(CreateItem(items[5], StoreProgress.Shadow, 150, "Shadow", shadowIcon,
            "PASS THROUGH PLATFORMS.\nLAND ON THEM FROM ABOVE.", -1));
        catalog.Sort((a, b) =>
        {
            int priceOrder = a.price.CompareTo(b.price);
            return priceOrder != 0 ? priceOrder : string.Compare(
                a.button.transform.parent.Find("Item Name").GetComponent<TMP_Text>().text,
                b.button.transform.parent.Find("Item Name").GetComponent<TMP_Text>().text,
                StringComparison.OrdinalIgnoreCase);
        });
        items = catalog.ToArray();
        foreach (Item item in items) item.button.transform.parent.SetAsLastSibling();
        CreateTabs();
        SelectCategory(Category.General);
    }

    private Item CreateItem(Item template, string id, int price, string displayName,
        Sprite portrait, string description, int siblingIndex)
    {
        Transform sourceCard = template.button.transform.parent;
        GameObject card = Instantiate(sourceCard.gameObject, sourceCard.parent);
        card.name = displayName;
        if (siblingIndex >= 0) card.transform.SetSiblingIndex(siblingIndex);

        card.transform.Find("Item Name").GetComponent<TMP_Text>().text = displayName.ToUpperInvariant();
        card.transform.Find("Description").GetComponent<TMP_Text>().text = description;
        card.transform.Find("Price").GetComponent<TMP_Text>().text = $"{price} COINS";
        Image image = card.transform.Find("Icon Plate/Portrait").GetComponent<Image>();
        Animator animator = image.GetComponent<Animator>();
        if (animator != null) animator.enabled = false;
        image.sprite = portrait;
        if (id == StoreProgress.Bravy || id == StoreProgress.Volty || id == StoreProgress.Marinee || id == StoreProgress.Cyclops || id == StoreProgress.Dash || id == StoreProgress.Shadow) UiSpriteAnimation.Attach(image, portrait);
        if (id == StoreProgress.Cheese) CreateCheeseShadow(image);

        Button button = card.GetComponentInChildren<Button>(true);
        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener(() => Buy(id));
        return new Item
        {
            id = id,
            price = price,
            button = button,
            buttonLabel = button.GetComponentInChildren<TMP_Text>(true)
        };
    }

    private void CreateTabs()
    {
        catalogScroll = items[0].button.GetComponentInParent<ScrollRect>();
        RectTransform frame = (RectTransform)catalogScroll.transform;
        RectTransform viewport = catalogScroll.viewport;
        viewport.offsetMax = new Vector2(viewport.offsetMax.x, viewport.offsetMax.y - 50f);
        string[] names = { "GENERAL", "CHARMS", "MARBLES" };
        for (int i = 0; i < names.Length; i++)
        {
            Category category = (Category)i;
            GameObject tab = new GameObject(names[i] + " Tab", typeof(RectTransform), typeof(Image), typeof(Button));
            RectTransform rect = tab.GetComponent<RectTransform>();
            rect.SetParent(frame, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(30f + i * 116f, -20f);
            rect.sizeDelta = new Vector2(108f, 32f);
            Image background = tab.GetComponent<Image>();
            tabImages[i] = background;
            Button button = tab.GetComponent<Button>();
            button.targetGraphic = background;
            button.onClick.AddListener(() => SelectCategory(category));

            GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.SetParent(rect, false);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
            TMP_Text label = labelObject.GetComponent<TMP_Text>();
            label.font = balanceLabel.font;
            label.fontSize = 15f;
            label.enableAutoSizing = true;
            label.fontSizeMin = 10f;
            label.fontSizeMax = 15f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(1f, 0.94f, 0.83f);
            label.raycastTarget = false;
            label.text = names[i];
        }
    }

    private void SelectCategory(Category category)
    {
        selectedCategory = category;
        foreach (Item item in items)
        {
            bool marble = StoreProgress.IsMarble(item.id);
            item.button.transform.parent.gameObject.SetActive(category == Category.General ||
                (category == Category.Marbles ? marble : !marble));
        }
        for (int i = 0; i < tabImages.Length; i++)
            tabImages[i].color = (Category)i == selectedCategory
                ? new Color(0.28f, 0.54f, 0.64f) : new Color(0.14f, 0.21f, 0.34f);
        messageLabel.text = "";
        Canvas.ForceUpdateCanvases();
        catalogScroll.verticalNormalizedPosition = 1f;
        catalogScroll.StopMovement();
    }

    private static void CreateCheeseShadow(Image icon)
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
        shadow.sprite = icon.sprite;
        shadow.color = new Color(0.015f, 0.025f, 0.075f, 0.9f);
        shadow.preserveAspect = true;
        shadow.raycastTarget = false;
    }

    private void OnEnable()
    {
        Refresh();
    }

    public void Buy(string id)
    {
        Item item = Array.Find(items, entry => entry.id == id);
        if (item == null) return;

        if (StoreProgress.IsOwned(id)) return;

        int coins = PlayerPrefs.GetInt(StoreProgress.Coins, 0);
        if (coins < item.price)
        {
            messageLabel.text = "NOT ENOUGH COINS";
            return;
        }

        PlayerPrefs.SetInt(StoreProgress.Coins, coins - item.price);
        PlayerPrefs.SetInt(id, 1);
        messageLabel.text = "UNLOCKED!";

        PlayerPrefs.Save();
        Refresh();
    }

    private void Refresh()
    {
        balanceLabel.text = "COINS  " + PlayerPrefs.GetInt(StoreProgress.Coins, 0);
        foreach (Item item in items)
        {
            bool owned = StoreProgress.IsOwned(item.id);
            if (!owned)
            {
                item.buttonLabel.text = "BUY";
                item.button.interactable = true;
                item.buttonLabel.color = new Color32(255, 241, 214, 255);
            }
            else
            {
                item.buttonLabel.text = "YOURS";
                item.button.interactable = false;
                item.buttonLabel.color = new Color32(145, 151, 164, 255);
            }
        }
    }
}
