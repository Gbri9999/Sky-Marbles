
using System.Collections;
using TMPro;
using UnityEngine;

public partial class GameManager : MonoBehaviour
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

    public float timeBetweenEvents = 10f;

    [Header("Alertas")]
    [SerializeField] private CanvasGroup warningPanel;
    [SerializeField] private string earthquakeAlert = "WARNING: TERREMOTO";
    [SerializeField] private string mirrorAlert = "WARNING: ESPEJO - A/D INVERTIDOS";
    [SerializeField] private string laserAlert = "WARNING: LASERES";

    private PlayerController playerController;
    private CameraMovement cameraMovement;
    private Rigidbody2D playerBody;
    private GameObject laser;
    private Sprite laserSprite;
    private Sprite laserGlowSprite;
    private float nextEvent;
    private int eventIndex;
    private TMP_Text warningText;

    void Start()
    {
        playerController = player.GetComponent<PlayerController>();
        playerBody = player.GetComponent<Rigidbody2D>();
        cameraMovement = Camera.main.GetComponent<CameraMovement>();
        warningText = warningPanel.GetComponentInChildren<TMP_Text>();
        warningPanel.alpha = 0f;
        GenerateChunk(currentChunk * platformsPerChunk);
        currentChunk++;
    }

    void Update()
    {
        if (isGameOver)
            return;

        if (nextEvent == 0f)
        {
            if (playerController.alreadyJumped)
                nextEvent = Time.time + Mathf.Max(1f, timeBetweenEvents);
        }
        else if (Time.time >= nextEvent)
        {
            nextEvent = Time.time + Mathf.Max(1f, timeBetweenEvents);
            StartCoroutine(RunEvent(eventIndex++ % 3));
        }

        float nextChunkTrigger = (currentChunk * platformsPerChunk - 4) * heightChunkDifference;

        if (player.position.y >= nextChunkTrigger)
        {
            GenerateChunk(currentChunk * platformsPerChunk);
            currentChunk++;
        }
    }

    private IEnumerator RunEvent(int type)
    {
        warningText.text = type == 0 ? earthquakeAlert : type == 1 ? mirrorAlert : laserAlert;
        PlayGameplaySound(warningSound);
        float elapsed = 0f;
        while (elapsed < 1f)
        {
            elapsed += Time.deltaTime;
            warningPanel.alpha = Mathf.Min(1f, elapsed * 6f);
            warningPanel.transform.localScale = Vector3.one * Mathf.Lerp(1.12f, 1f, Mathf.Clamp01(elapsed * 5f));
            yield return null;
        }

        yield return type == 0 ? Earthquake() : type == 1 ? Mirror() : Lasers();
        warningPanel.alpha = 0f;
    }

    private IEnumerator Earthquake()
    {
        PlayGameplaySound(earthquakeSound);
        cameraMovement.Shake(2f, 0.25f);
        for (int i = 0; i < 10 && !isGameOver; i++)
        {
            playerBody.AddForce(Vector2.right * Random.Range(-1.5f, 1.5f), ForceMode2D.Impulse);
            yield return new WaitForSeconds(0.2f);
        }
    }

    private IEnumerator Mirror()
    {
        yield return Flip(true);
        yield return new WaitForSeconds(2.3f);
        yield return Flip(false);
    }

    private IEnumerator Flip(bool mirrored)
    {
        float elapsed = 0f;
        while (elapsed < 0.35f)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / 0.35f);
            cameraMovement.SetMirrorScale((mirrored ? 1f : -1f) * Mathf.Cos(progress * Mathf.PI));
            playerController.controlsMirrored = mirrored ? progress >= 0.5f : progress < 0.5f;
            yield return null;
        }
        cameraMovement.SetMirror(mirrored);
        playerController.controlsMirrored = mirrored;
    }

    private void PrepareLaserSprites()
    {
        if (laserSprite != null) return;
        Texture2D white = Texture2D.whiteTexture;
        laserSprite = Sprite.Create(white, new Rect(0, 0, white.width, white.height), new Vector2(0.5f, 0.5f), white.width);

        Texture2D texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        Color32[] pixels = new Color32[32 * 32];
        for (int i = 0; i < pixels.Length; i++)
        {
            float fade = 1f - Mathf.Abs((i % 32 - 15.5f) / 15.5f);
            pixels[i] = new Color32(255, 255, 255, (byte)(255f * fade * fade));
        }
        texture.SetPixels32(pixels);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.Apply(false, true);
        laserGlowSprite = Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32f);
    }

    private IEnumerator Lasers()
    {
        PrepareLaserSprites();
        float lastPlayerX = player.position.x;
        float side = Mathf.Abs(lastPlayerX) > 2.2f ? -Mathf.Sign(lastPlayerX) : (Random.value < 0.5f ? -1f : 1f);

        laser = new GameObject("Lasers");
        laser.transform.SetParent(cameraMovement.transform, false);
        laser.transform.localPosition = new Vector3(0f, 0f, 10f);
        SpriteRenderer[] beams = new SpriteRenderer[2];
        SpriteRenderer[] glows = new SpriteRenderer[2];
        Transform[] sparks = new Transform[2];

        for (int i = 0; i < 2; i++)
        {
            GameObject ray = new GameObject("Laser");
            ray.transform.SetParent(laser.transform, false);
            float distance = i == 0 ? Random.Range(0.9f, 1.4f) : Random.Range(1.9f, 2.6f);
            ray.transform.localPosition = new Vector3(Mathf.Clamp(lastPlayerX + side * distance, -4.3f, 4.3f), 0f, 0f);
            ray.transform.localScale = new Vector3(0.16f, 12f, 1f);

            beams[i] = ray.AddComponent<SpriteRenderer>();
            beams[i].sprite = laserSprite;
            beams[i].color = new Color(1f, 0.06f, 0.05f, 0.8f);
            beams[i].sortingOrder = 21;

            GameObject glow = new GameObject("Glow");
            glow.transform.SetParent(ray.transform, false);
            glow.transform.localScale = new Vector3(7f, 1f, 1f);
            SpriteRenderer halo = glow.AddComponent<SpriteRenderer>();
            halo.sprite = laserGlowSprite;
            halo.color = new Color(1f, 0.05f, 0.04f, 0.6f);
            halo.sortingOrder = 20;
            glows[i] = halo;

            GameObject core = new GameObject("Core");
            core.transform.SetParent(ray.transform, false);
            core.transform.localScale = new Vector3(0.3f, 1f, 1f);
            SpriteRenderer center = core.AddComponent<SpriteRenderer>();
            center.sprite = laserSprite;
            center.color = new Color(0.8f, 1f, 1f);
            center.sortingOrder = 22;
            center.enabled = false;

            GameObject spark = new GameObject("Spark");
            spark.transform.SetParent(ray.transform, false);
            spark.transform.localScale = new Vector3(3f, 0.03f, 1f);
            SpriteRenderer flash = spark.AddComponent<SpriteRenderer>();
            flash.sprite = laserGlowSprite;
            flash.color = new Color(0.8f, 1f, 1f, 0.9f);
            flash.sortingOrder = 23;
            flash.enabled = false;
            sparks[i] = spark.transform;

            BoxCollider2D hitbox = ray.AddComponent<BoxCollider2D>();
            hitbox.isTrigger = true;
            hitbox.enabled = false;
        }
        float elapsed = 0f;
        while (elapsed < 1.2f)
        {
            elapsed += Time.deltaTime;
            float flash = Mathf.PingPong(elapsed * 5f, 1f) > 0.5f ? 1f : 0.18f;
            for (int i = 0; i < beams.Length; i++)
            {
                beams[i].color = new Color(1f, 0.06f, 0.05f, 0.8f * flash);
                glows[i].color = new Color(1f, 0.05f, 0.04f, 0.6f * flash);
            }
            yield return null;
        }

        if (isGameOver) yield break;
        for (int i = 0; i < beams.Length; i++)
        {
            Transform ray = beams[i].transform;
            ray.localScale = new Vector3(0.22f, 12f, 1f);
            beams[i].color = new Color(0.05f, 0.85f, 1f, 0.9f);
            glows[i].color = new Color(0.1f, 0.7f, 1f, 0.75f);
            ray.GetChild(1).GetComponent<SpriteRenderer>().enabled = true;
            sparks[i].GetComponent<SpriteRenderer>().enabled = true;
            ray.GetComponent<BoxCollider2D>().enabled = true;
        }
        elapsed = 0f;
        while (elapsed < 2.2f)
        {
            elapsed += Time.deltaTime;
            for (int i = 0; i < beams.Length; i++)
            {
                glows[i].transform.localScale = new Vector3(5f + 0.7f * Mathf.Sin(elapsed * 14f + i), 1f, 1f);
                sparks[i].localPosition = new Vector3(0f, Mathf.Repeat(elapsed * 0.9f + i * 0.5f, 1f) - 0.5f, 0f);
            }
            yield return null;
        }
        Destroy(laser);
        laser = null;
    }

    public bool IsLaser(Collider2D other) => laser != null && other.transform.parent == laser.transform;

    private void OnDestroy()
    {
        if (laserGlowSprite != null)
        {
            Destroy(laserGlowSprite.texture);
            Destroy(laserGlowSprite);
        }
        if (laserSprite != null) Destroy(laserSprite);
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
            EndSceneIntro();
            PlayGameplaySound(deathSound);
            gameOverController.ShowGameOver();
            StopAllCoroutines();
            warningPanel.alpha = 0f;
            playerController.controlsMirrored = false;
            cameraMovement.SetMirror(false);
            cameraMovement.Shake(0f, 0f);
            if (laser != null) Destroy(laser);
            laser = null;
        }
    }
}
