
using UnityEngine;
using TMPro;

public class Coin : MonoBehaviour
{
    public GameManager manager;
    public TMP_Text coinText;

    void Awake()
    {
        manager = GameObject.Find("GameManager").GetComponent<GameManager>();
        coinText = GameObject.Find("Coin Text").GetComponent<TMP_Text>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            manager.coins++;
            coinText.text = "Coins: " + manager.coins;

            Destroy(gameObject);
        }
    }
}
