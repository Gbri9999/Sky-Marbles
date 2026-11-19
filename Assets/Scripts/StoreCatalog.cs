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
    [SerializeField] private Sprite cheeseIcon;
    [SerializeField] private Sprite cyclopsIcon;
    [SerializeField] private Sprite dashIcon;

    private void Awake()
    {
        List<Item> catalog = new List<Item>(items);
        catalog.Add(CreateItem(items[2], StoreProgress.Bravy, 70, "Bravy", bravyPortrait,
            "STRONGER JUMP.\nBALANCED GRAVITY.", 3));
        catalog.Add(CreateItem(items[2], StoreProgress.Volty, 100, "Volty", voltyPortrait,
            "12% STRONGER JUMP, 2X SPEED.\nFLOATY FALL + EXTRA JUMP.", 4));
        catalog.Add(CreateItem(items[5], StoreProgress.Cheese, 90, "Cheese", cheeseIcon,
            "SUMMON A BOUNCY CHEESE BED.", -1));
        catalog.Add(CreateItem(items[5], StoreProgress.Cyclops, 120, "Cyclops", cyclopsIcon,
            "FOLLOWING COMPANION.\nBOUNCE ON IT AGAIN AND AGAIN.", -1));
        catalog.Add(CreateItem(items[5], StoreProgress.Dash, 100, "Dash", dashIcon,
            "AIM WITH WASD OR ARROWS.\nDASH AND REBOUND.", -1));
        items = catalog.ToArray();
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
        if (id == StoreProgress.Bravy || id == StoreProgress.Volty || id == StoreProgress.Cyclops || id == StoreProgress.Dash) UiSpriteAnimation.Attach(image, portrait);
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
