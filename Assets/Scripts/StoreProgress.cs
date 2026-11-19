using UnityEngine;

public static class StoreProgress
{
    public const string Coins = "SaveCoins";
    public const string Bluey = "Bluey";
    public const string Breeze = "Store.Breeze";
    public const string Bouncy = "Store.Bouncy";
    public const string Bravy = "Store.Bravy";
    public const string Volty = "Store.Volty";
    public const string DoubleJump = "Store.DoubleJump";
    public const string Magnet = "Store.Magnet";
    public const string PatrickTotem = "Store.PatrickTotem";
    public const string Cheese = "Store.Cheese";
    public const string Cyclops = "Store.Cyclops";
    public const string Dash = "Store.Dash";
    public const string SelectedCharacter = "Store.SelectedCharacter";
    public const string AmuletSlot1 = "Store.AmuletSlot1";
    public const string AmuletSlot2 = "Store.AmuletSlot2";

    public static bool IsOwned(string id)
    {
        return id == Bluey || PlayerPrefs.GetInt(id, 0) == 1;
    }

    public static bool IsEquipped(string id)
    {
        return IsOwned(id) &&
               (PlayerPrefs.GetString(AmuletSlot1, "") == id ||
                PlayerPrefs.GetString(AmuletSlot2, "") == id);
    }

    public static bool IsActiveAmulet(string id)
    {
        return id == Cheese || id == Dash;
    }

    public static bool UsesSecondaryActiveKey(string id)
    {
        string first = PlayerPrefs.GetString(AmuletSlot1, "");
        return PlayerPrefs.GetString(AmuletSlot2, "") == id &&
               IsActiveAmulet(first) && IsOwned(first);
    }

    public static int ActiveHudIndex(string id)
    {
        return UsesSecondaryActiveKey(id) ? 1 : 0;
    }
}
