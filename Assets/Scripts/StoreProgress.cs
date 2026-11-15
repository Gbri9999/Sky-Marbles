using UnityEngine;

public static class StoreProgress
{
    public const string Coins = "SaveCoins";
    public const string Bluey = "Bluey";
    public const string Breeze = "Store.Breeze";
    public const string Bouncy = "Store.Bouncy";
    public const string DoubleJump = "Store.DoubleJump";
    public const string Magnet = "Store.Magnet";
    public const string PatrickTotem = "Store.PatrickTotem";
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
}
