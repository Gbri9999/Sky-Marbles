using UnityEngine;
using TMPro;

public class SaveSystem : MonoBehaviour
{
    public GameManager manager;
    [SerializeField] private TMP_Text coinText;

    private bool loaded;

    private void Awake()
    {
        LoadData();
        loaded = true;
    }

    public void SaveData()
    {
        PlayerPrefs.SetInt(StoreProgress.Coins, manager.coins);
        PlayerPrefs.Save();
    }

    public void LoadData()
    {
        manager.coins = PlayerPrefs.GetInt(StoreProgress.Coins, 0);
        coinText.text = "COINS  " + manager.coins;
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused && loaded) SaveData();
    }

    private void OnApplicationQuit()
    {
        if (loaded) SaveData();
    }

    private void OnDisable()
    {
        if (loaded) SaveData();
    }
}
