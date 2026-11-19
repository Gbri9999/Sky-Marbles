
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

    [Header("Run generation")]
    [SerializeField] private bool useRandomSeed = true;
    [SerializeField] private int seed;
    public int Seed => seed;

    [Header("Alertas")]
    [SerializeField] private CanvasGroup warningPanel;
    [SerializeField] private string earthquakeAlert = "WARNING: EARTHQUAKE";
    [SerializeField] private string mirrorAlert = "WARNING: MIRROR - A/D REVERSED";
    [SerializeField] private string laserAlert = "WARNING: LASERS";

    private PlayerController playerController;
    private CameraMovement cameraMovement;
    private Rigidbody2D playerBody;
    private GameObject laser;
    private Sprite laserSprite;
    private Sprite laserGlowSprite;
    private float nextEvent;
    private System.Random platformRandom;
    private System.Random eventRandom;
    private TMP_Text warningText;
    private bool voltyCoinBonus;
    private float previousPlatformX;
    private bool hasPreviousPlatform;

    void Start()
    {
        if (useRandomSeed) seed = System.Guid.NewGuid().GetHashCode();
        platformRandom = new System.Random(unchecked(seed ^ 0x51A73E2D));
        eventRandom = new System.Random(unchecked(seed ^ 0x2B19C4F7));
        playerController = player.GetComponent<PlayerController>();
        voltyCoinBonus = PlayerPrefs.GetString(StoreProgress.SelectedCharacter, StoreProgress.Bluey) ==
                         StoreProgress.Volty && StoreProgress.IsOwned(StoreProgress.Volty);
        playerBody = player.GetComponent<Rigidbody2D>();
        cameraMovement = Camera.main.GetComponent<CameraMovement>();
        cameraMovement.GenerateStars(seed);
        cameraMovement.GeneratePlanets(seed);
        warningText = warningPanel.GetComponentInChildren<TMP_Text>();
        warningPanel.alpha = 0f;
        InitializeAltitudeMilestones();
        GenerateChunk(currentChunk * platformsPerChunk);
        currentChunk++;
    }

    void Update()
    {
        if (isGameOver)
            return;

        UpdateAltitudeMilestones();

        if (nextEvent == 0f)
        {
            if (playerController.alreadyJumped)
                ScheduleNextEvent();
        }
        else if (Time.time >= nextEvent)
        {
            nextEvent = float.PositiveInfinity;
            StartCoroutine(RunEvent(eventRandom.Next(0, 3)));
        }

        float nextChunkTrigger = PlatformHeight(currentChunk * platformsPerChunk - 4);

        if (player.position.y >= nextChunkTrigger)
        {
            GenerateChunk(currentChunk * platformsPerChunk);
            currentChunk++;
        }
    }

    private void ScheduleNextEvent()
    {
        float interval = timeBetweenEvents * Mathf.Lerp(0.65f, 1.4f, (float)eventRandom.NextDouble());
        nextEvent = Time.time + Mathf.Max(1f, interval);
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
        if (!isGameOver) ScheduleNextEvent();
    }

    private IEnumerator Earthquake()
    {
        PlayGameplaySound(earthquakeSound);
        cameraMovement.Shake(2f, 0.25f);
        bool brokePlatform = false;
        float elapsed = 0f;
        float nextImpulse = 0f;
        while (elapsed < 2f && !isGameOver)
        {
            if (!brokePlatform && playerController.TryGetSupportingPlatform(out GameObject platform) &&
                platform.GetComponent<CyclopsCompanion>() == null)
            {
                PlatformFracture.Play(platform, eventRandom);
                platform.SetActive(false);
                Destroy(platform);
                brokePlatform = true;
            }

            if (elapsed >= nextImpulse)
            {
                float push = Mathf.Lerp(-1.5f, 1.5f, (float)eventRandom.NextDouble());
                playerBody.AddForce(Vector2.right * push, ForceMode2D.Impulse);
                nextImpulse += 0.2f;
            }

            yield return new WaitForFixedUpdate();
            elapsed += Time.fixedDeltaTime;
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
        float side = Mathf.Abs(lastPlayerX) > 2.2f ? -Mathf.Sign(lastPlayerX) :
            (eventRandom.NextDouble() < 0.5 ? -1f : 1f);

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
            float distance = i == 0
                ? Mathf.Lerp(0.9f, 1.4f, (float)eventRandom.NextDouble())
                : Mathf.Lerp(1.9f, 2.6f, (float)eventRandom.NextDouble());
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
        DisposeAltitudeMilestones();
        if (laserGlowSprite != null)
        {
            Destroy(laserGlowSprite.texture);
            Destroy(laserGlowSprite);
        }
        if (laserSprite != null) Destroy(laserSprite);
    }

    private float PlatformHeight(int index)
    {
        // Use the original spacing after 1000 meters, without a gap at the transition.
        float easierSpacing = heightChunkDifference * 0.9f;
        float transitionIndex = (startingAltitude + 1000f) / easierSpacing;
        return index <= transitionIndex ? index * easierSpacing :
            startingAltitude + 1000f + (index - transitionIndex) * heightChunkDifference;
    }

    void GenerateChunk(int startIndex)
    {
        BoxCollider2D platformCollider = platformPrefab.GetComponent<BoxCollider2D>();
        float platformWidth = platformCollider != null
            ? platformCollider.size.x * Mathf.Abs(platformPrefab.transform.localScale.x) : 3f;
        const float edge = 4.7f;
        for (int i = 0; i < platformsPerChunk; i++)
        {
            float height = PlatformHeight(startIndex + i);
            float x = Mathf.Lerp(-edge, edge, (float)platformRandom.NextDouble());
            if (height - startingAltitude <= 1000f && hasPreviousPlatform)
            {
                // Sample from the portions that do not overlap the platform below.
                float separation = Mathf.Min(platformWidth + 0.1f, edge * 2f);
                float leftStart = Mathf.Max(-edge, previousPlatformX - 5f);
                float leftEnd = Mathf.Max(leftStart, previousPlatformX - separation);
                float rightEnd = Mathf.Min(edge, previousPlatformX + 5f);
                float rightStart = Mathf.Min(rightEnd, previousPlatformX + separation);
                float leftLength = leftEnd - leftStart;
                float rightLength = rightEnd - rightStart;
                float sample = (float)platformRandom.NextDouble() * (leftLength + rightLength);
                x = sample < leftLength ? leftStart + sample : rightStart + sample - leftLength;
            }
            previousPlatformX = x;
            hasPreviousPlatform = true;
            Vector3 pos = new Vector3(x, height, 0f);
            float coinX = Mathf.Lerp(-edge, edge, (float)platformRandom.NextDouble());

            Instantiate(platformPrefab, pos, Quaternion.identity, platformContainer.transform);

            bool regularCoin = i % 3 == 0;
            bool extraVoltyCoin = voltyCoinBonus && (i % 3 == 1 || i == 8);
            if (regularCoin || extraVoltyCoin)
            {
                // Each 10 meters adds 0.1 percentage points to the chance of a nearby coin.
                float nearbyChance = Mathf.Clamp01(Mathf.Floor(Mathf.Max(0f, height - startingAltitude) / 10f) * 0.001f);
                if (platformRandom.NextDouble() < nearbyChance)
                    coinX = Mathf.Clamp(x + Mathf.Lerp(-1f, 1f, (float)platformRandom.NextDouble()), -edge, edge);
                Instantiate(coinPrefab, new Vector3(coinX, height + 1f, 0f),
                    Quaternion.identity, coinContainer.transform);
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
            HideAltitudeMilestone();
            warningPanel.alpha = 0f;
            playerController.controlsMirrored = false;
            cameraMovement.SetMirror(false);
            cameraMovement.Shake(0f, 0f);
            if (laser != null) Destroy(laser);
            laser = null;
        }
    }
}
