using UnityEngine;

public class SaveSystem : MonoBehaviour
{
    public GameManager manager;

    public void SaveData()
    {
        PlayerPrefs.SetInt("SaveCoins", manager.coins);
        PlayerPrefs.Save(); 
    }

    public void LoadData()
    {
        manager.coins = PlayerPrefs.GetInt("SaveCoins", 0);
    }
}