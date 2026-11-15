using UnityEngine;

[RequireComponent(typeof(Camera))]
public class JumpSpeedLines : MonoBehaviour
{
    [Header("Jump speed lines")]
    [SerializeField, Range(6, 24)] private int lineCount = 12;
    [SerializeField, Min(0.1f)] private float effectDuration = 0.48f;
    [SerializeField] private Color cyan = new Color(0.72f, 0.86f, 0.89f, 0.24f);
    [SerializeField] private Color pink = new Color(0.95f, 0.66f, 0.77f, 0.18f);

    private Camera sceneCamera;
    private Texture2D lineTexture;
    private Sprite lineSprite;
    private SpriteRenderer[] lines;
    private float[] lineSpeeds;
    private Color[] lineColors;
    private float timeRemaining;

    private void Awake()
    {
        sceneCamera = GetComponent<Camera>();
        lineTexture = new Texture2D(1, 64, TextureFormat.RGBA32, false);
        lineTexture.filterMode = FilterMode.Point;
        lineTexture.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < 64; y++)
        {
            float position = y / 63f;
            float firstStreak = Mathf.Clamp01(1f - Mathf.Abs(position - 0.25f) / 0.18f);
            float secondStreak = Mathf.Clamp01(1f - Mathf.Abs(position - 0.75f) / 0.12f);
            float opacity = 0.4f + 0.6f * Mathf.Max(firstStreak, secondStreak);
            lineTexture.SetPixel(0, y, new Color(1f, 1f, 1f, opacity));
        }
        lineTexture.Apply();
        lineSprite = Sprite.Create(lineTexture, new Rect(0f, 0f, 1f, 64f),
            new Vector2(0.5f, 0.5f), 64f);

        int count = Mathf.Clamp(lineCount, 6, 24);
        lines = new SpriteRenderer[count];
        lineSpeeds = new float[count];
        lineColors = new Color[count];

        for (int i = 0; i < count; i++)
        {
            GameObject line = new GameObject("Jump Speed Line " + (i + 1), typeof(SpriteRenderer));
            line.transform.SetParent(transform, false);
            SpriteRenderer renderer = line.GetComponent<SpriteRenderer>();
            renderer.sprite = lineSprite;
            renderer.sortingOrder = 25;
            renderer.enabled = false;
            lines[i] = renderer;
        }
    }

    public void Play()
    {
        if (lines == null) return;

        timeRemaining = Mathf.Max(0.1f, effectDuration);
        float halfHeight = sceneCamera.orthographicSize;
        float halfWidth = halfHeight * sceneCamera.aspect;
        float usableHalfWidth = halfWidth * 0.78f;
        float spacing = usableHalfWidth * 2f / lines.Length;

        for (int i = 0; i < lines.Length; i++)
        {
            SpriteRenderer renderer = lines[i];
            float x = -usableHalfWidth + spacing * (i + 0.5f) +
                Random.Range(-spacing * 0.14f, spacing * 0.14f);
            lineSpeeds[i] = Random.Range(8f, 13f);
            float travel = lineSpeeds[i] * timeRemaining;
            float fullScreenLength = halfHeight * 2f + travel + 0.3f;
            renderer.transform.localPosition = new Vector3(x, travel * 0.5f, 9f);
            // The sprite is 1/64 world units wide and one world unit tall.
            renderer.transform.localScale = new Vector3(Random.Range(0.025f, 0.045f) * 64f,
                fullScreenLength, 1f);

            Color color = i % 3 == 0 ? pink : cyan;
            color.a *= Random.Range(0.7f, 1f);
            lineColors[i] = color;
            renderer.color = color;
            renderer.enabled = true;
        }
    }

    private void Update()
    {
        if (timeRemaining <= 0f) return;

        timeRemaining = Mathf.Max(0f, timeRemaining - Time.deltaTime);
        float opacity = timeRemaining / Mathf.Max(0.1f, effectDuration);

        for (int i = 0; i < lines.Length; i++)
        {
            SpriteRenderer renderer = lines[i];
            renderer.transform.localPosition += Vector3.down * (lineSpeeds[i] * Time.deltaTime);
            Color color = lineColors[i];
            color.a *= opacity;
            renderer.color = color;
            renderer.enabled = timeRemaining > 0f;
        }
    }

    private void OnDestroy()
    {
        if (lineSprite != null) Destroy(lineSprite);
        if (lineTexture != null) Destroy(lineTexture);
    }
}
