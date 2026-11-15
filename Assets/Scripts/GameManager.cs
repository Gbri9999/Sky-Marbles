
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public GameObject platformContainer;
    public GameObject platformPrefab;
    public Transform player;
    public GameObject coinPrefab;
    public GameObject coinContainer;

    public float heightChunkDifference;

    private int currentChunk = 0;
    private const int platformsPerChunk = 10;

    public GameOver gameOverController;

    public bool isGameOver;
    public int coins;

    void Start()
    {
        GenerateChunk(currentChunk * platformsPerChunk);
        currentChunk++;
    }

    void Update()
    {
        if (isGameOver)
            return;

        float nextChunkTrigger =
            (currentChunk * platformsPerChunk - 4) * heightChunkDifference;

        if (player.position.y >= nextChunkTrigger)
        {
            GenerateChunk(currentChunk * platformsPerChunk);
            currentChunk++;
        }
    }

    void GenerateChunk(int startIndex)
    {
        for (int i = 0; i < platformsPerChunk; i++)
        {
            Vector3 pos = new Vector3(
                Random.Range(-4.7f, 4.7f),
                (startIndex + i) * heightChunkDifference,
                0
            );

            Vector3 posCoin = new Vector3(
                Random.Range(-4.7f, 4.7f),
                ((startIndex + i) * heightChunkDifference) + 1,
                0
            );

            Instantiate(
                platformPrefab,
                pos,
                Quaternion.identity,
                platformContainer.transform
            );

            if(i%3 == 0)
            {
                Instantiate(
                    coinPrefab,
                    posCoin,
                    Quaternion.identity,
                    coinContainer.transform
                );
            }
        }
    }

    public void GameOver()
    {
        if (!isGameOver)
        {
            gameOverController.ShowGameOver();
        }
    }
}