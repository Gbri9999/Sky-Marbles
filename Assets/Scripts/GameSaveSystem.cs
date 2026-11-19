using UnityEngine;

public static class GameSaveSystem
{
    private const string LoreCompletedSceneKey = "Lore.CompletedScene";

    public static void MarkLoreCompleted(int sceneIndex)
    {
        PlayerPrefs.SetInt(LoreCompletedSceneKey, sceneIndex);
        PlayerPrefs.Save();
    }

    public static void ClearSaveData()
    {
        PlayerPrefs.DeleteKey(StoreProgress.Coins);
        PlayerPrefs.DeleteKey(StoreProgress.Breeze);
        PlayerPrefs.DeleteKey(StoreProgress.Bouncy);
        PlayerPrefs.DeleteKey(StoreProgress.Bravy);
        PlayerPrefs.DeleteKey(StoreProgress.Volty);
        PlayerPrefs.DeleteKey(StoreProgress.DoubleJump);
        PlayerPrefs.DeleteKey(StoreProgress.Magnet);
        PlayerPrefs.DeleteKey(StoreProgress.PatrickTotem);
        PlayerPrefs.DeleteKey(StoreProgress.Cheese);
        PlayerPrefs.DeleteKey(StoreProgress.Cyclops);
        PlayerPrefs.DeleteKey(StoreProgress.Dash);
        PlayerPrefs.DeleteKey(StoreProgress.SelectedCharacter);
        PlayerPrefs.DeleteKey(StoreProgress.AmuletSlot1);
        PlayerPrefs.DeleteKey(StoreProgress.AmuletSlot2);
        PlayerPrefs.DeleteKey(LoreCompletedSceneKey);
        PlayerPrefs.Save();
    }
}
