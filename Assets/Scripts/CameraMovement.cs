using System.Collections.Generic;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    public Transform player;
    public PlayerController controller;

    public float smoothTimeUp = 0.24f;
    public float smoothTimeDown = 0.22f;

    public float upThreshold = 1.1f;
    public float downThreshold = 1.5f;

    [Header("Background by altitude")]
    [SerializeField, Min(1f)] private float heightToDarkestColor = 170f;
    [SerializeField] private Color middleBackgroundColor = new Color(0.13f, 0.10f, 0.28f, 1f);
    [SerializeField] private Color darkestBackgroundColor = new Color(0.012f, 0.018f, 0.065f, 1f);
    [SerializeField, Range(32, 160)] private int starCount = 96;
    [SerializeField] private Sprite planetSourceSprite;

    private float cameraVelocity;
    private float targetY;
    private Vector2 shakeOffset;
    private float shakeEnd;
    private float shakeDuration;
    private float shakeStrength;
    private Camera cameraComponent;
    private Color startingBackgroundColor;
    private float startingPlayerHeight;
    private float highestPlayerHeight;
    private Sprite starSprite;
    private Star[] stars;
    private float lastAspect;
    private int planetSeed;
    private Sprite[] planetSprites;
    private Transform planetRoot;
    private readonly Dictionary<int, Planet> planets = new Dictionary<int, Planet>();
    private readonly List<int> planetsToRemove = new List<int>();

    private struct Planet
    {
        public SpriteRenderer renderer;
        public float anchorY;
        public float x;
        public float opacity;
        public float phase;
    }

    private struct Star
    {
        public SpriteRenderer renderer;
        public Vector2 screenPosition;
        public Color color;
        public float size;
        public float phase;
        public float twinkleSpeed;
        public float revealProgress;
    }

    void Awake()
    {
        cameraComponent = GetComponent<Camera>();
        targetY = transform.position.y;
        startingBackgroundColor = cameraComponent.backgroundColor;
        startingPlayerHeight = player.position.y;
        highestPlayerHeight = startingPlayerHeight;
    }

    public void GenerateStars(int seed)
    {
        if (stars != null) return;

        const int textureSize = 16;
        Texture2D texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        Color32[] pixels = new Color32[textureSize * textureSize];
        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                float dx = (x - 7.5f) / 7.5f;
                float dy = (y - 7.5f) / 7.5f;
                float radius = Mathf.Sqrt(dx * dx + dy * dy);
                float glow = Mathf.Pow(Mathf.Clamp01(1f - radius), 2.2f);
                pixels[y * textureSize + x] = new Color32(255, 255, 255, (byte)(255f * glow));
            }
        }
        texture.SetPixels32(pixels);
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.Apply(false, true);
        starSprite = Sprite.Create(texture, new Rect(0, 0, textureSize, textureSize),
            new Vector2(0.5f, 0.5f), textureSize);

        Transform starRoot = new GameObject("Procedural Stars").transform;
        starRoot.SetParent(transform, false);
        starRoot.localPosition = new Vector3(0f, 0f, 10f);

        System.Random random = new System.Random(unchecked(seed ^ 0x6C8E9CF5));
        stars = new Star[starCount];
        for (int i = 0; i < stars.Length; i++)
        {
            GameObject starObject = new GameObject("Star");
            starObject.transform.SetParent(starRoot, false);
            SpriteRenderer renderer = starObject.AddComponent<SpriteRenderer>();
            renderer.sprite = starSprite;
            renderer.sortingOrder = -20;

            double colorChoice = random.NextDouble();
            Color tint = colorChoice < 0.62 ? new Color(0.89f, 0.94f, 1f) :
                colorChoice < 0.84 ? new Color(0.68f, 0.78f, 1f) : new Color(1f, 0.79f, 0.98f);
            stars[i] = new Star
            {
                renderer = renderer,
                screenPosition = new Vector2((float)random.NextDouble() * 2f - 1f,
                    (float)random.NextDouble() * 2f - 1f),
                color = tint,
                size = Mathf.Lerp(0.10f, 0.27f, (float)random.NextDouble()),
                phase = (float)random.NextDouble() * Mathf.PI * 2f,
                twinkleSpeed = Mathf.Lerp(1.3f, 4.2f, (float)random.NextDouble()),
                revealProgress = Mathf.Lerp(0.12f, 0.72f, (float)random.NextDouble())
            };
            renderer.color = new Color(tint.r, tint.g, tint.b, 0f);
            starObject.transform.localScale = Vector3.one * stars[i].size;
        }
        PositionStars();
    }

    public void GeneratePlanets(int seed)
    {
        if (planetSourceSprite == null || planetSprites != null) return;
        planetSeed = seed;
        Color[] palette =
        {
            new Color(1f, 0.48f, 0.55f),
            new Color(1f, 0.72f, 0.43f),
            new Color(0.72f, 0.55f, 0.95f),
            new Color(0.45f, 0.78f, 0.96f),
            new Color(0.96f, 0.58f, 0.83f)
        };

        Texture2D source = planetSourceSprite.texture;
        Color32[] sourcePixels = source.GetPixels32();
        planetSprites = new Sprite[palette.Length];
        for (int variant = 0; variant < palette.Length; variant++)
        {
            Texture2D texture = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[sourcePixels.Length];
            Color tint = palette[variant];
            for (int i = 0; i < pixels.Length; i++)
            {
                Color32 original = sourcePixels[i];
                float shade = Mathf.Max(original.r, original.g, original.b) / 255f;
                pixels[i] = new Color32(
                    (byte)(255f * tint.r * shade),
                    (byte)(255f * tint.g * shade),
                    (byte)(255f * tint.b * shade), original.a);
            }
            texture.SetPixels32(pixels);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.Apply(false, true);
            planetSprites[variant] = Sprite.Create(texture,
                new Rect(0f, 0f, source.width, source.height),
                new Vector2(0.5f, 0.5f), planetSourceSprite.pixelsPerUnit);
        }

        planetRoot = new GameObject("Distant Planets").transform;
        planetRoot.SetParent(transform, false);
        planetRoot.localPosition = new Vector3(0f, 0f, 10f);
    }

    private void UpdatePlanets()
    {
        if (planetSprites == null) return;
        float climbed = highestPlayerHeight - startingPlayerHeight;
        float cameraClimbed = transform.position.y - startingPlayerHeight;
        const float firstHeight = 500f;
        const float spacing = 8f;
        const float visibleRange = 21f;
        int first = Mathf.Max(0, Mathf.FloorToInt((cameraClimbed - firstHeight - visibleRange) / spacing));
        int last = Mathf.FloorToInt((cameraClimbed - firstHeight + visibleRange) / spacing);

        planetsToRemove.Clear();
        foreach (KeyValuePair<int, Planet> pair in planets)
            if (pair.Key < first || pair.Key > last) planetsToRemove.Add(pair.Key);
        foreach (int index in planetsToRemove)
        {
            Destroy(planets[index].renderer.gameObject);
            planets.Remove(index);
        }

        if (climbed < firstHeight) return;
        for (int index = first; index <= last; index++)
        {
            if (planets.ContainsKey(index)) continue;
            System.Random random = new System.Random(unchecked(planetSeed ^ (index * 1301081)));
            GameObject planetObject = new GameObject("Planet " + index);
            planetObject.transform.SetParent(planetRoot, false);
            SpriteRenderer renderer = planetObject.AddComponent<SpriteRenderer>();
            renderer.sprite = planetSprites[(index * 3 + (planetSeed & 0x7fffffff)) % planetSprites.Length];
            renderer.sortingOrder = -18;
            planetObject.transform.localScale = Vector3.one * Mathf.Lerp(0.7f, 1.65f,
                (float)random.NextDouble());
            planets[index] = new Planet
            {
                renderer = renderer,
                anchorY = startingPlayerHeight + firstHeight + index * spacing +
                    Mathf.Lerp(-2f, 2f, (float)random.NextDouble()),
                x = Mathf.Lerp(-3.8f, 3.8f, (float)random.NextDouble()),
                opacity = Mathf.Lerp(0.36f, 0.56f, (float)random.NextDouble()),
                phase = (float)random.NextDouble() * Mathf.PI * 2f
            };
        }

        float reveal = Mathf.Lerp(0.08f, 1f,
            Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((climbed - firstHeight) / 320f)));
        foreach (Planet planet in planets.Values)
        {
            planet.renderer.transform.localPosition = new Vector3(planet.x,
                (planet.anchorY - transform.position.y) * 0.35f, 0f);
            float shimmer = 0.92f + 0.08f * Mathf.Sin(Time.time * 0.7f + planet.phase);
            planet.renderer.color = new Color(1f, 1f, 1f, planet.opacity * reveal * shimmer);
        }
    }

    private void PositionStars()
    {
        lastAspect = cameraComponent.aspect;
        float halfHeight = cameraComponent.orthographicSize;
        float halfWidth = halfHeight * lastAspect;
        for (int i = 0; i < stars.Length; i++)
        {
            Vector2 position = stars[i].screenPosition;
            stars[i].renderer.transform.localPosition = new Vector3(
                position.x * halfWidth, position.y * halfHeight, 0f);
        }
    }

    public void Shake(float duration, float strength)
    {
        shakeDuration = duration;
        shakeStrength = strength;
        shakeEnd = Time.time + duration;
    }

    public void SetMirror(bool active)
    {
        if (active)
            SetMirrorScale(-1f);
        else
            cameraComponent.ResetProjectionMatrix();
    }

    public void SetMirrorScale(float scale)
    {
        cameraComponent.ResetProjectionMatrix();
        float visibleScale = Mathf.Sign(scale) * Mathf.Max(0.03f, Mathf.Abs(scale));
        cameraComponent.projectionMatrix = Matrix4x4.Scale(new Vector3(visibleScale, 1f, 1f)) * cameraComponent.projectionMatrix;
    }

    void LateUpdate()
    {
        Vector3 position = transform.position - (Vector3)shakeOffset;
        float cameraY = position.y;

        if (controller != null && controller.inSector)
        {
            cameraVelocity = 0f;
            targetY = cameraY;
        }
        else
        {
            float playerY = player.position.y;
            if (playerY > cameraY + upThreshold)
                targetY = playerY - upThreshold;
            else if (playerY < cameraY - downThreshold)
                targetY = playerY + downThreshold;

            float currentSmoothTime = targetY < cameraY ? smoothTimeDown : smoothTimeUp;
            position.y = Mathf.SmoothDamp(cameraY, targetY, ref cameraVelocity, currentSmoothTime);
        }

        shakeOffset = Time.time < shakeEnd
            ? Random.insideUnitCircle * (shakeStrength * (shakeEnd - Time.time) / shakeDuration)
            : Vector2.zero;
        transform.position = position + (Vector3)shakeOffset;

        highestPlayerHeight = Mathf.Max(highestPlayerHeight, player.position.y);
        float darkness = Mathf.Clamp01((highestPlayerHeight - startingPlayerHeight) / heightToDarkestColor);
        Color middle = middleBackgroundColor;
        Color cosmic = darkestBackgroundColor;
        middle.a = startingBackgroundColor.a;
        cosmic.a = startingBackgroundColor.a;
        cameraComponent.backgroundColor = darkness < 0.45f
            ? Color.Lerp(startingBackgroundColor, middle, Mathf.SmoothStep(0f, 1f, darkness / 0.45f))
            : Color.Lerp(middle, cosmic, Mathf.SmoothStep(0f, 1f, (darkness - 0.45f) / 0.55f));

        UpdatePlanets();

        if (stars == null) return;
        if (!Mathf.Approximately(lastAspect, cameraComponent.aspect)) PositionStars();
        for (int i = 0; i < stars.Length; i++)
        {
            Star star = stars[i];
            float reveal = Mathf.SmoothStep(0f, 1f,
                Mathf.Clamp01((darkness - star.revealProgress) / 0.22f));
            float twinkle = 0.68f + 0.32f * Mathf.Sin(Time.time * star.twinkleSpeed + star.phase);
            Color color = star.color;
            color.a = reveal * twinkle;
            star.renderer.color = color;
        }
    }

    private void OnDestroy()
    {
        if (starSprite != null)
        {
            Destroy(starSprite.texture);
            Destroy(starSprite);
        }
        if (planetSprites == null) return;
        foreach (Sprite sprite in planetSprites)
        {
            Destroy(sprite.texture);
            Destroy(sprite);
        }
    }
}
