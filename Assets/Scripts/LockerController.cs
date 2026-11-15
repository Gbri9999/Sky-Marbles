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

    private Sprite magnetIconSprite;

    private static readonly string[] SlotKeys = { StoreProgress.AmuletSlot1, StoreProgress.AmuletSlot2 };

    private void Awake()
    {
        magnetIconSprite = amuletSlots[0].magnetIcon.sprite;
    }

    private void OnEnable()
    {
        CleanInvalidEquipment();
        Refresh();
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
        Refresh();
    }

    public void PreviousAmulet1() => CycleAmulet(0, -1);
    public void NextAmulet1() => CycleAmulet(0, 1);
    public void PreviousAmulet2() => CycleAmulet(1, -1);
    public void NextAmulet2() => CycleAmulet(1, 1);

    private void CycleAmulet(int slot, int direction)
    {
        List<string> options = AvailableAmulets();
        int current = options.IndexOf(PlayerPrefs.GetString(SlotKeys[slot], ""));
        int next = (current + direction + options.Count) % options.Count;
        string chosen = options[next];

        if (chosen != "" && PlayerPrefs.GetString(SlotKeys[1 - slot], "") == chosen)
            PlayerPrefs.SetString(SlotKeys[1 - slot], "");

        PlayerPrefs.SetString(SlotKeys[slot], chosen);
        PlayerPrefs.Save();
        Refresh();
    }

    private List<string> AvailableAmulets()
    {
        List<string> options = new List<string> { "" };
        if (StoreProgress.IsOwned(StoreProgress.DoubleJump)) options.Add(StoreProgress.DoubleJump);
        if (StoreProgress.IsOwned(StoreProgress.Magnet)) options.Add(StoreProgress.Magnet);
        if (StoreProgress.IsOwned(StoreProgress.PatrickTotem)) options.Add(StoreProgress.PatrickTotem);
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
                                   selected == StoreProgress.Bouncy ? "BOUNCY" : "BLUEY";

        foreach (MarbleView marble in marbles)
        {
            bool owned = IsMarbleOwned(marble.id);
            marble.card.alpha = owned ? 1f : 0.4f;
            marble.button.interactable = owned;
            marble.status.text = !owned ? "LOCKED" : selected == marble.id ? "EQUIPPED" : "DRAG OR TAP";
            marble.slotPreview.SetActive(selected == marble.id);
        }

        List<string> options = AvailableAmulets();
        for (int i = 0; i < 2; i++)
        {
            string id = PlayerPrefs.GetString(SlotKeys[i], "");
            AmuletView view = amuletSlots[i];
            view.nameLabel.text = id == StoreProgress.DoubleJump ? "DOUBLE JUMP" :
                                  id == StoreProgress.Magnet ? "MAGNET" :
                                  id == StoreProgress.PatrickTotem ? "PATRICK TOTEM" : "EMPTY";
            view.jumpIcon.gameObject.SetActive(id == StoreProgress.DoubleJump);
            view.magnetIcon.sprite = id == StoreProgress.PatrickTotem ? patrickTotemIcon : magnetIconSprite;
            view.magnetIcon.gameObject.SetActive(id == StoreProgress.Magnet || id == StoreProgress.PatrickTotem);
            view.previous.interactable = options.Count > 1;
            view.next.interactable = options.Count > 1;
        }
    }
}
