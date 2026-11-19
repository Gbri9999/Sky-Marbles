using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(Collider2D))]
public class CheesePower : MonoBehaviour
{
    [SerializeField] private Sprite cheeseIcon;
    [SerializeField] private Sprite cheeseBedSprite;
    [SerializeField] private RuntimeAnimatorController cheeseBedAnimation;
    [SerializeField, Min(1f)] private float cooldownSeconds = 12f;
    [SerializeField, Min(1f)] private float bedLifetime = 8f;

    private GameManager manager;
    private Collider2D playerCollider;
    private bool equipped;
    private bool useSecondaryKey;
    private float readyAt;
    private float blockedUntil;
    private RectTransform hud;
    private Image cooldownRing;
    private TMP_Text cooldownLabel;
    private Sprite ringSprite;

    private void Awake()
    {
        equipped = StoreProgress.IsEquipped(StoreProgress.Cheese);
        useSecondaryKey = StoreProgress.UsesSecondaryActiveKey(StoreProgress.Cheese);
        manager = GetComponent<PlayerController>().manager;
        playerCollider = GetComponent<Collider2D>();
    }

    private void Start()
    {
        if (equipped) CreateCooldownDisplay();
    }

    private void Update()
    {
        if (!equipped || manager == null || manager.isGameOver || Time.timeScale <= 0f) return;

        float remaining = Mathf.Max(0f, readyAt - Time.time);
        cooldownRing.fillAmount = 1f - Mathf.Clamp01(remaining / cooldownSeconds);
        cooldownRing.color = Time.time < blockedUntil
            ? new Color(1f, 0.4f, 0.4f)
            : remaining > 0f ? new Color(0.55f, 0.78f, 0.96f) :
                new Color(1f, 0.86f, 0.42f);
        cooldownLabel.text = Time.time < blockedUntil ? "BLOCKED" :
            remaining > 0f ? Mathf.CeilToInt(remaining) + "s" : "READY";

        Keyboard keyboard = Keyboard.current;
        bool pressed = keyboard != null && (useSecondaryKey
            ? (keyboard.leftShiftKey.wasPressedThisFrame || keyboard.rightShiftKey.wasPressedThisFrame) : keyboard.eKey.wasPressedThisFrame);
        if (pressed && remaining <= 0f)
        {
            if (TryPlaceBed()) readyAt = Time.time + cooldownSeconds;
            else blockedUntil = Time.time + 0.7f;
        }
    }

    private bool TryPlaceBed()
    {
        if (cheeseBedSprite == null || cheeseBedAnimation == null) return false;

        Bounds bounds = playerCollider.bounds;
        Vector2 bedPosition = new Vector2(bounds.center.x, bounds.min.y - 0.85f);
        Collider2D[] nearby = Physics2D.OverlapBoxAll(bedPosition, new Vector2(2.7f, 1.45f), 0f);
        foreach (Collider2D collider in nearby)
        {
            if (collider == playerCollider) continue;
            if (collider.CompareTag("Platforms") || collider.CompareTag("Floor")) return false;
        }

        GameObject bed = new GameObject("Cheese Bed");
        bed.tag = "Platforms";
        bed.transform.position = new Vector3(bedPosition.x, bedPosition.y, 0f);
        bed.transform.localScale = new Vector3(2.4f, 1f, 1f);
        SpriteRenderer renderer = bed.AddComponent<SpriteRenderer>();
        renderer.sprite = cheeseBedSprite;
        renderer.sortingOrder = 2;
        GameObject shadow = new GameObject("Cheese Shadow");
        shadow.transform.SetParent(bed.transform, false);
        shadow.transform.localPosition = new Vector3(0.035f, -0.075f, 0.01f);
        SpriteRenderer shadowRenderer = shadow.AddComponent<SpriteRenderer>();
        shadowRenderer.sprite = cheeseBedSprite;
        shadowRenderer.color = new Color(0.02f, 0.025f, 0.075f, 0.82f);
        shadowRenderer.sortingOrder = renderer.sortingOrder - 1;
        Animator animator = bed.AddComponent<Animator>();
        animator.runtimeAnimatorController = cheeseBedAnimation;
        BoxCollider2D platform = bed.AddComponent<BoxCollider2D>();
        platform.size = new Vector2(0.88f, 0.26f);
        bed.AddComponent<CheeseBed>().Initialize(bedLifetime);
        return true;
    }

    private void CreateCooldownDisplay()
    {
        TMP_Text coinText = GameObject.Find("Coin Text").GetComponent<TMP_Text>();
        RectTransform canvas = coinText.GetComponentInParent<Canvas>().GetComponent<RectTransform>();
        GameObject root = new GameObject("Cheese Cooldown", typeof(RectTransform));
        hud = root.GetComponent<RectTransform>();
        hud.SetParent(canvas, false);
        hud.anchorMin = hud.anchorMax = new Vector2(1f, 0f);
        hud.pivot = new Vector2(1f, 0f);
        hud.anchoredPosition = new Vector2(-28f, 138f - 110f * StoreProgress.ActiveHudIndex(StoreProgress.Cheese));
        hud.sizeDelta = new Vector2(100f, 112f);

        ringSprite = CreatePixelRing();
        Image back = CreateImage(hud, "Ring Back", new Vector2(0f, 12f),
            new Vector2(82f, 82f), ringSprite, new Color(0.15f, 0.24f, 0.34f, 0.9f));
        back.type = Image.Type.Simple;
        cooldownRing = CreateImage(hud, "Recharge", new Vector2(0f, 12f),
            new Vector2(82f, 82f), ringSprite, new Color(1f, 0.86f, 0.42f));
        cooldownRing.type = Image.Type.Filled;
        cooldownRing.fillMethod = Image.FillMethod.Radial360;
        cooldownRing.fillOrigin = (int)Image.Origin360.Top;
        cooldownRing.fillClockwise = true;
        cooldownRing.fillAmount = 1f;

        CreateImage(hud, "Cheese Shadow", new Vector2(3f, 9f), new Vector2(56f, 56f),
            cheeseIcon, new Color(0.015f, 0.025f, 0.08f, 0.95f)).preserveAspect = true;
        CreateImage(hud, "Cheese", new Vector2(0f, 12f), new Vector2(52f, 52f),
            cheeseIcon, Color.white).preserveAspect = true;
        CreateImage(hud, "E Badge", new Vector2(32f, 45f), new Vector2(24f, 24f),
            null, new Color(0.09f, 0.14f, 0.27f, 0.95f));
        CreateLabel(hud, useSecondaryKey ? "SHIFT" : "E", new Vector2(32f, 45f), new Vector2(24f, 24f),
            coinText.font, 16f, new Color(1f, 0.91f, 0.64f));
        cooldownLabel = CreateLabel(hud, "READY", new Vector2(0f, -37f),
            new Vector2(95f, 22f), coinText.font, 14f, new Color(0.87f, 0.96f, 1f));
    }

    internal static Image CreateImage(RectTransform parent, string name, Vector2 position,
        Vector2 size, Sprite sprite, Color color)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Image image = gameObject.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    internal static TMP_Text CreateLabel(RectTransform parent, string value, Vector2 position,
        Vector2 size, TMP_FontAsset font, float fontSize, Color color)
    {
        GameObject gameObject = new GameObject(value, typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        TMP_Text label = gameObject.GetComponent<TextMeshProUGUI>();
        label.font = font;
        label.fontSize = fontSize;
        label.alignment = TextAlignmentOptions.Center;
        label.color = color;
        label.raycastTarget = false;
        label.text = value;
        return label;
    }

    internal static Sprite CreatePixelRing()
    {
        const int size = 48;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color32[] pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - 23.5f;
                float dy = y - 23.5f;
                float radius = Mathf.Sqrt(dx * dx + dy * dy);
                pixels[y * size + x] = new Color32(255, 255, 255,
                    (byte)(radius >= 18f && radius <= 23f ? 255 : 0));
            }
        }
        texture.SetPixels32(pixels);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.Apply(false, true);
        return Sprite.Create(texture, new Rect(0f, 0f, size, size),
            new Vector2(0.5f, 0.5f), size);
    }

    private void OnDestroy()
    {
        if (hud != null) Destroy(hud.gameObject);
        if (ringSprite == null) return;
        Destroy(ringSprite.texture);
        Destroy(ringSprite);
    }
}
