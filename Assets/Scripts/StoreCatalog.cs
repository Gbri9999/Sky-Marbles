using System;
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
