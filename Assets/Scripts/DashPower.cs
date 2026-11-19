using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody2D))]
public class DashPower : MonoBehaviour
{
    [SerializeField] private Sprite dashIcon;
    [SerializeField] private RuntimeAnimatorController dashAnimation;
    [SerializeField, Min(1f)] private float dashSpeed = 22f;
    [SerializeField, Min(0.05f)] private float dashDuration = 0.16f;
    [SerializeField, Min(0.1f)] private float cooldownSeconds = 5f;
    [SerializeField, Min(1f)] private float reboundSpeed = 21f;

    private Rigidbody2D body;
    private PlayerController player;
    private GameManager manager;
    private bool equipped;
    private bool useSecondaryKey;
    private bool dashing;
    private float dashEndsAt;
    private float readyAt;
    private float previousGravity;
    private Vector2 dashDirection;
    private SpriteRenderer playerRenderer;
    private Material originalMaterial;
    private Material flashMaterial;
    private float flashUntil;
    private RectTransform hud;
    private Image cooldownRing;
    private TMP_Text cooldownLabel;
    private Sprite ringSprite;

    private void Awake()
    {
        equipped = StoreProgress.IsEquipped(StoreProgress.Dash);
        useSecondaryKey = StoreProgress.UsesSecondaryActiveKey(StoreProgress.Dash);
        body = GetComponent<Rigidbody2D>();
        player = GetComponent<PlayerController>();
        if (player == null) { enabled = false; return; }
        manager = player.manager;
        playerRenderer = GetComponent<SpriteRenderer>();
        Shader shader = Shader.Find("SkyMarbles/DashFlash");
        if (playerRenderer != null && shader != null) { originalMaterial = playerRenderer.sharedMaterial; flashMaterial = new Material(shader); }
    }

    private void Start()
    {
        if (!equipped) return;

        CreateCooldownDisplay();
    }

    private void Update()
    {
        if (!equipped) return;
        if (playerRenderer != null && flashUntil > 0f)
        {
            float fade = Mathf.Clamp01((flashUntil - Time.time) / 0.22f);
            if (flashMaterial != null) flashMaterial.SetFloat("_Flash", fade);
            if (fade <= 0f) { playerRenderer.sharedMaterial = originalMaterial; flashUntil = 0f; }
        }
        if (manager == null || manager.isGameOver)
        {
            if (dashing) EndDash(false);
            return;
        }
        if (Time.timeScale <= 0f) return;

        if (dashing && Time.time >= dashEndsAt) EndDash(false);
        float remaining = Mathf.Max(0f, readyAt - Time.time);
        cooldownRing.fillAmount = 1f - Mathf.Clamp01(remaining / cooldownSeconds);
        cooldownRing.color = dashing ? new Color(1f, 0.95f, 0.5f) :
            remaining > 0f ? new Color(0.55f, 0.78f, 0.96f) :
            new Color(1f, 0.86f, 0.42f);
        cooldownLabel.text = dashing ? "DASH!" :
            remaining > 0f ? Mathf.CeilToInt(remaining) + "s" : "READY";

        Keyboard keyboard = Keyboard.current;
        bool pressed = MobileControls.ConsumeActive(StoreProgress.ActiveHudIndex(StoreProgress.Dash)) || keyboard != null && (useSecondaryKey
            ? (keyboard.leftShiftKey.wasPressedThisFrame || keyboard.rightShiftKey.wasPressedThisFrame) : keyboard.eKey.wasPressedThisFrame);
        if (pressed && remaining <= 0f && !dashing)
            BeginDash(MobileControls.IsMobile ? MobileControls.DashDirection(player.LastHorizontalDirection) : ReadDirection(keyboard));
    }

    private Vector2 ReadDirection(Keyboard keyboard)
    {
        float x = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1f : 0f) -
                  (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1f : 0f);
        float y = (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1f : 0f) -
                  (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1f : 0f);
        Vector2 direction = new Vector2(x, y);
        if (direction.sqrMagnitude > 0.01f) return direction.normalized;
        return player.LastHorizontalDirection == 0
            ? Vector2.up : new Vector2(player.LastHorizontalDirection, 0f);
    }

    private void BeginDash(Vector2 direction)
    {
        dashDirection = direction;
        dashEndsAt = Time.time + dashDuration;
        readyAt = Time.time + cooldownSeconds;
        previousGravity = body.gravityScale;
        dashing = true;
        player.SetDashMovement(true);
        body.gravityScale = 0f;
        body.linearVelocity = dashDirection * dashSpeed;
        if (playerRenderer != null && flashMaterial != null) { playerRenderer.sharedMaterial = flashMaterial; flashMaterial.SetFloat("_Flash", 1f); flashUntil = Time.time + 0.22f; }
    }

    private void FixedUpdate()
    {
        if (!dashing) return;
        if (Time.time >= dashEndsAt)
        {
            EndDash(false);
            return;
        }
        body.linearVelocity = dashDirection * dashSpeed;
    }

    private void OnCollisionEnter2D(Collision2D collision) => HandleCollision(collision);
    private void OnCollisionStay2D(Collision2D collision) => HandleCollision(collision);

    private void HandleCollision(Collision2D collision)
    {
        if (!dashing) return;
        for (int i = 0; i < collision.contactCount; i++)
        {
            Vector2 normal = collision.GetContact(i).normal;
            bool platform = collision.gameObject.CompareTag("Platforms") ||
                            collision.gameObject.CompareTag("Floor");
            if (platform && dashDirection.y < -0.1f && normal.y > 0.35f)
            {
                EndDash(true);
                body.linearVelocity = new Vector2(dashDirection.x, -dashDirection.y).normalized *
                    reboundSpeed;
                return;
            }
            if (Vector2.Dot(normal, dashDirection) < -0.35f)
            {
                EndDash(false);
                return;
            }
        }
    }

    private void EndDash(bool rebounded)
    {
        if (!dashing) return;
        dashing = false;
        body.gravityScale = previousGravity;
        player.SetDashMovement(false, rebounded);

        if (!rebounded) body.linearVelocity *= 0.45f;
    }

    private void CreateCooldownDisplay()
    {
        TMP_Text coinText = GameObject.Find("Coin Text").GetComponent<TMP_Text>();
        RectTransform canvas = coinText.GetComponentInParent<Canvas>().GetComponent<RectTransform>();
        GameObject root = new GameObject("Dash Cooldown", typeof(RectTransform));
        hud = root.GetComponent<RectTransform>();
        hud.SetParent(canvas, false);
        hud.anchorMin = hud.anchorMax = new Vector2(1f, 0f);
        hud.pivot = new Vector2(1f, 0f);
        hud.anchoredPosition = new Vector2(-28f, 138f - 110f * StoreProgress.ActiveHudIndex(StoreProgress.Dash));
        hud.sizeDelta = new Vector2(100f, 112f);
        if (MobileControls.IsMobile) MobileControls.ConfigureActiveHud(hud, StoreProgress.ActiveHudIndex(StoreProgress.Dash));

        ringSprite = CheesePower.CreatePixelRing();
        CheesePower.CreateImage(hud, "Ring Back", new Vector2(0f, 12f),
            new Vector2(82f, 82f), ringSprite, new Color(0.15f, 0.24f, 0.34f, 0.9f));
        cooldownRing = CheesePower.CreateImage(hud, "Recharge", new Vector2(0f, 12f),
            new Vector2(82f, 82f), ringSprite, new Color(1f, 0.86f, 0.42f));
        cooldownRing.type = Image.Type.Filled;
        cooldownRing.fillMethod = Image.FillMethod.Radial360;
        cooldownRing.fillOrigin = (int)Image.Origin360.Top;
        cooldownRing.fillClockwise = true;
        cooldownRing.fillAmount = 1f;
        CheesePower.CreateImage(hud, "Sparky Shadow", new Vector2(3f, 9f),
            new Vector2(56f, 56f), dashIcon, new Color(0.015f, 0.025f, 0.08f, 0.95f)).preserveAspect = true;
        Image icon = CheesePower.CreateImage(hud, "Sparky", new Vector2(0f, 12f),
            new Vector2(52f, 52f), dashIcon, Color.white);
        icon.preserveAspect = true;
        UiSpriteAnimation.Attach(icon, dashIcon);
        CheesePower.CreateImage(hud, "Key Badge", new Vector2(32f, 45f),
            new Vector2(24f, 24f), null, new Color(0.09f, 0.14f, 0.27f, 0.95f));
        CheesePower.CreateLabel(hud, MobileControls.IsMobile ? "TAP" : useSecondaryKey ? "SHIFT" : "E",
            new Vector2(32f, 45f), new Vector2(24f, 24f), coinText.font,
            16f, new Color(1f, 0.91f, 0.64f));
        cooldownLabel = CheesePower.CreateLabel(hud, "READY", new Vector2(0f, -37f),
            new Vector2(95f, 22f), coinText.font, 14f, new Color(0.87f, 0.96f, 1f));
    }

    private void OnDisable()
    {
        if (dashing) EndDash(false);
    }

    private void OnDestroy()
    {
        if (playerRenderer != null && flashMaterial != null && playerRenderer.sharedMaterial == flashMaterial) playerRenderer.sharedMaterial = originalMaterial;
        if (flashMaterial != null) Destroy(flashMaterial);
        if (hud != null) Destroy(hud.gameObject);
        if (ringSprite == null) return;
        Destroy(ringSprite.texture);
        Destroy(ringSprite);
    }
}
