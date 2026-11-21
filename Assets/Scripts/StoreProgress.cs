using UnityEngine;

public static class StoreProgress
{
    public const string Coins = "SaveCoins";
    public const string Bluey = "Bluey";
    public const string Breeze = "Store.Breeze";
    public const string Bouncy = "Store.Bouncy";
    public const string Bravy = "Store.Bravy";
    public const string Marinee = "Store.Marinee";
    public const string Loki = "Store.Loki";
    public const string Hamburger = "Store.Hamburger";
    public const string MoneyBag = "Store.MoneyBag";
    public const string Volty = "Store.Volty";
    public const string DoubleJump = "Store.DoubleJump";
    public const string Magnet = "Store.Magnet";
    public const string Aspirator = "Store.Aspirator";
    public const string PatrickTotem = "Store.PatrickTotem";
    public const string Cheese = "Store.Cheese";
    public const string Cyclops = "Store.Cyclops";
    public const string Shadow = "Store.Shadow";
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

    public static bool IsMarble(string id)
    {
        return id == Bluey || id == Breeze || id == Bouncy || id == Bravy || id == Volty || id == Marinee || id == Loki;
    }

    public static void ConfigureShadowPlatform(Collider2D platform)
    {
        if (platform == null || !IsEquipped(Shadow)) return;
        PlatformEffector2D effector = platform.GetComponent<PlatformEffector2D>();
        if (effector == null) effector = platform.gameObject.AddComponent<PlatformEffector2D>();
        effector.useOneWay = true;
        effector.surfaceArc = 120f;
        platform.usedByEffector = true;
    }

    public static bool IsActiveAmulet(string id)
    {
        return id == Cheese || id == Dash || id == Hamburger;
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
