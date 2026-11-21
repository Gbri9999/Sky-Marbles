using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class PlayerController : MonoBehaviour
{
    private Rigidbody2D rb;
    private Collider2D playerCollider;
    public GameManager manager;

    private bool isGrounded;

    public float naturalBounceForce = 6f;
    public float bounceforce = 6f;
    public float wallBounce = 5f;

    private int direction;
    public float velocity = 6f;
    private float newVelocity;
    public float smoothTime = 0.08f;
    private float velocityRef;
    private bool isWallBouncing;
    [HideInInspector] public bool controlsMirrored;

    [Header("Jump Assistance")]
    public float jumpBufferTime = 0.15f;
    public float coyoteTime = 0.12f;
    public bool holdSpaceForJump = true;

    [Header("Marble Powers")]
    [SerializeField, Range(0.2f, 0.9f)] private float breezeGravityMultiplier = 0.65f;
    [SerializeField, Range(1f, 1.5f)] private float bravyJumpMultiplier = 1.15f;

    [Header("Ground Detection")]
    public float groundCheckDistance = 0.12f;

    private float jumpBufferCounter;
    private float coyoteTimeCounter;
    private bool jumpUsedSinceLanding;
    private bool holdingSpace;
    private int lastHorizontalDirection;
    private bool coyoteFromCyclops;
    private bool dashMovementActive;
    private float preserveHorizontalUntil;
    private int extraJumpsPerLanding;
    private int extraJumpsRemaining;
    private float jumpForceMultiplier = 1f;
    private bool voltyEquipped;
    [SerializeField] private Sprite hamburgerIcon;
    private bool lokiEquipped;
    private float lokiParryReady;
    private bool marineeEquipped;
    private int marineeAirJumpsUsed;
    private float normalGravityScale;
    private bool shadowEquipped;
    private bool patrickTotemEquipped;
    private bool patrickTotemUsed;

    private ContactFilter2D groundFilter;
    private readonly RaycastHit2D[] groundHits = new RaycastHit2D[12];
    private readonly Vector2[] groundOrigins = new Vector2[3];
    private GameObject supportingPlatform;
    private float lastSupportedFixedTime = float.NegativeInfinity;
    private float lastLandingBounceTime = float.NegativeInfinity;

    public bool inSector;
    private int sectorsInside;

    public bool alreadyJumped;
    private bool isDead;
    public int LastHorizontalDirection => lastHorizontalDirection;
    public float LokiJumpImpulse => bounceforce * 1.18f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        normalGravityScale = rb.gravityScale;
        playerCollider = GetComponent<Collider2D>();
        shadowEquipped = StoreProgress.IsEquipped(StoreProgress.Shadow);
        patrickTotemEquipped = StoreProgress.IsEquipped(StoreProgress.PatrickTotem);
        string marble = PlayerPrefs.GetString(StoreProgress.SelectedCharacter, StoreProgress.Bluey);
        if (!StoreProgress.IsOwned(marble)) marble = StoreProgress.Bluey;
        extraJumpsPerLanding = marble == StoreProgress.Bouncy && StoreProgress.IsOwned(StoreProgress.Bouncy) ? 2 : 0;
        if (marble == StoreProgress.Volty)
        {
            voltyEquipped = true;
            velocity *= 2f;
            jumpForceMultiplier = 1.12f;
            extraJumpsPerLanding++;
        }
        else if (marble == StoreProgress.Loki)
        {
            lokiEquipped = true;
            rb.gravityScale *= Mathf.Clamp(breezeGravityMultiplier, 0.2f, 0.9f);
            velocity *= 1.2f;
            jumpForceMultiplier = 1.18f;
            extraJumpsPerLanding = 2;
        }
        else if (marble == StoreProgress.Marinee)
        {
            marineeEquipped = true;
            rb.gravityScale *= Mathf.Clamp(breezeGravityMultiplier, 0.2f, 0.9f);
            jumpForceMultiplier = 1.12f;
            extraJumpsPerLanding = 2;
        }
        else if (marble == StoreProgress.Bravy)
        {
            rb.gravityScale *= (1f + Mathf.Clamp(breezeGravityMultiplier, 0.2f, 0.9f)) * 0.5f;
            jumpForceMultiplier = bravyJumpMultiplier;
        }
        if (StoreProgress.IsEquipped(StoreProgress.DoubleJump))
            extraJumpsPerLanding++;
        extraJumpsRemaining = extraJumpsPerLanding;
        if (marble == StoreProgress.Breeze && StoreProgress.IsOwned(StoreProgress.Breeze))
        {
            float gravityMultiplier = Mathf.Clamp(breezeGravityMultiplier, 0.2f, 0.9f);
            rb.gravityScale *= gravityMultiplier;
            jumpForceMultiplier = Mathf.Sqrt(gravityMultiplier);
        }
        
        groundFilter = new ContactFilter2D();
        groundFilter.useTriggers = false;
    }

    void Update()
    {
        if(isDead) return;

        Keyboard keyboard = Keyboard.current;
        if (MobileControls.IsMobile)
        {
            direction = MobileControls.Aim.x > 0.2f ? 1 : MobileControls.Aim.x < -0.2f ? -1 : 0;
            if (controlsMirrored) direction = -direction;
            if (direction != 0) lastHorizontalDirection = direction;
            holdingSpace = MobileControls.JumpHeld;
            if (MobileControls.ConsumeJump()) jumpBufferCounter = jumpBufferTime;
            return;
        }
        if(keyboard == null)
        {
            direction = 0;
            holdingSpace = false;
            return;
        }

        bool leftPressed = keyboard.aKey.isPressed;
        bool rightPressed = keyboard.dKey.isPressed;
        if(leftPressed && !rightPressed)
        {
            direction = -1;
        }
        else if(rightPressed && !leftPressed)
        {
            direction = 1;
        }
        else direction = 0;

        if (controlsMirrored) direction = -direction;
        if (direction != 0) lastHorizontalDirection = direction;

        holdingSpace = keyboard.spaceKey.isPressed;
        
        if(keyboard.spaceKey.wasPressedThisFrame)
        {
            jumpBufferCounter = jumpBufferTime;
        }
    }

    private void Start()
    {
        if (MobileControls.IsMobile) gameObject.AddComponent<MobileControls>();
        if (StoreProgress.IsEquipped(StoreProgress.Hamburger)) gameObject.AddComponent<HamburgerPower>().Initialize(hamburgerIcon);
    }

    void FixedUpdate()
    {
        if(isDead || dashMovementActive) return;
        
        if(!isWallBouncing && Time.time >= preserveHorizontalUntil)
        {
            newVelocity = Mathf.SmoothDamp(
                rb.linearVelocity.x,
                velocity * direction,
                ref velocityRef,
                Mathf.Max(0.01f, smoothTime),
                Mathf.Infinity,
                Time.fixedDeltaTime
            );

            rb.linearVelocity = new Vector2(newVelocity, rb.linearVelocity.y);
        }
        else
        {
            newVelocity = rb.linearVelocity.x;
            velocityRef = 0f;
        }

        bool wasGrounded = isGrounded;
        
        GameObject platformUnderfoot = null;
        isGrounded = rb.linearVelocity.y <= 0.08f && CheckGround(out platformUnderfoot);
        supportingPlatform = isGrounded ? platformUnderfoot : null;
        if (supportingPlatform != null) lastSupportedFixedTime = Time.fixedTime;

        if(isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
            coyoteFromCyclops = supportingPlatform != null &&
                                supportingPlatform.GetComponent<CyclopsCompanion>() != null;
            
            // A moving support or a shallow collision can cancel a bounce before the
            // ground ray clears. Retry once the character has settled on the surface.
            bool stalledOnSurface = wasGrounded && rb.linearVelocity.y <= 0.01f &&
                                    Time.fixedTime - lastLandingBounceTime >= 0.08f;
            if(!wasGrounded || stalledOnSurface)
            {
                lastLandingBounceTime = Time.fixedTime;
                jumpUsedSinceLanding = false;
                extraJumpsRemaining = extraJumpsPerLanding;
                marineeAirJumpsUsed = 0;

                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
                CheeseBed cheeseBed = supportingPlatform != null
                    ? supportingPlatform.GetComponent<CheeseBed>() : null;
                float landingImpulse = cheeseBed != null
                    ? bounceforce * (1f + bravyJumpMultiplier) * 0.5f
                    : coyoteFromCyclops ? bounceforce * jumpForceMultiplier * 0.5f
                    : naturalBounceForce * jumpForceMultiplier;
                rb.AddForce(Vector2.up * landingImpulse, ForceMode2D.Impulse);
                if (coyoteFromCyclops && lastHorizontalDirection != 0)
                    rb.AddForce(Vector2.right * (lastHorizontalDirection * 2f), ForceMode2D.Impulse);
                if (cheeseBed != null) cheeseBed.Consume();
            }
        }
        else
        {
            coyoteTimeCounter = Mathf.Max(0f, coyoteTimeCounter - Time.fixedDeltaTime);
        }
        
        bool wantsToJump = jumpBufferCounter > 0f ||
                           (holdSpaceForJump && holdingSpace && isGrounded);

        if(wantsToJump && coyoteTimeCounter > 0f && !jumpUsedSinceLanding)
        {
            float jumpImpulse = bounceforce * jumpForceMultiplier *
                                (coyoteFromCyclops ? 0.5f : 1f);
            rb.AddForce(Vector2.up * jumpImpulse, ForceMode2D.Impulse);
            if (lokiEquipped) LokiSparkle.Play(transform.position);
            if (jumpBufferCounter > 0f && manager != null) manager.PlayJumpFeedback();

            alreadyJumped = true;
            jumpUsedSinceLanding = true;
            jumpBufferCounter = 0f;
            coyoteTimeCounter = 0f;
        }

        if(jumpBufferCounter > 0f && !isGrounded &&
           coyoteTimeCounter <= 0f && extraJumpsRemaining > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            float airJumpScale = lokiEquipped ? (marineeAirJumpsUsed == 0 ? 0.9f : 0.8f) : marineeEquipped && marineeAirJumpsUsed == 1 ? 0.55f : 1f;
            rb.AddForce(Vector2.up * bounceforce * jumpForceMultiplier * airJumpScale, ForceMode2D.Impulse);
            if (marineeEquipped || lokiEquipped) { marineeAirJumpsUsed++; if (lokiEquipped) LokiSparkle.Play(transform.position); else MarineeSparkle.Play(transform.position); }
            if (manager != null) manager.PlayJumpFeedback();
            alreadyJumped = true;
            extraJumpsRemaining--;
            jumpBufferCounter = 0f;
        }

        jumpBufferCounter = Mathf.Max(0f, jumpBufferCounter - Time.fixedDeltaTime);
        HamburgerPower hamburger = GetComponent<HamburgerPower>();
        if (voltyEquipped && (hamburger == null || !hamburger.IsFlying))
            rb.gravityScale = !isGrounded && rb.linearVelocity.y < -0.01f
                ? normalGravityScale * 0.5f : normalGravityScale;
    }

    public void BounceFromPufferFish()
    {
        if (isDead) return;
        DashPower dash = GetComponent<DashPower>();
        if (dash != null) dash.CancelForBounce();
        extraJumpsRemaining = extraJumpsPerLanding;
        marineeAirJumpsUsed = 0;
        jumpUsedSinceLanding = true;
        jumpBufferCounter = 0f;
        coyoteTimeCounter = 0f;
        isGrounded = false;
        alreadyJumped = true;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.up * (naturalBounceForce + bounceforce) *
            Mathf.Sqrt(Mathf.Clamp(breezeGravityMultiplier, 0.2f, 0.9f)), ForceMode2D.Impulse);
        if (manager != null) manager.PlayPufferFishBounceFeedback();
    }

    public bool TryGetSupportingPlatform(out GameObject platform)
    {
        platform = supportingPlatform;
        return !isDead && platform != null &&
               Time.fixedTime - lastSupportedFixedTime <= Time.fixedDeltaTime;
    }

    public void SetDashMovement(bool active, bool rebounded = false)
    {
        dashMovementActive = active;
        if (!active && rebounded) preserveHorizontalUntil = Time.time + 0.18f;
    }

    private bool CheckGround(out GameObject platform)
    {
        platform = null;
        Bounds bounds = playerCollider.bounds;
        float inset = Mathf.Min(0.08f, bounds.extents.x * 0.5f);
        
        float startY = bounds.min.y + 0.04f;
        float distance = groundCheckDistance + 0.04f;

        groundOrigins[0] = new Vector2(bounds.min.x + inset, startY);
        groundOrigins[1] = new Vector2(bounds.center.x, startY);
        groundOrigins[2] = new Vector2(bounds.max.x - inset, startY);

        bool grounded = false;
        for (int i = 0; i < groundOrigins.Length; i++)
        {
            Collider2D ground = GroundRay(groundOrigins[i], distance);
            if (ground == null) continue;

            grounded = true;
            if (ground.CompareTag("Platforms") || ground.GetComponent<CyclopsCompanion>() != null)
                platform = ground.gameObject;
        }

        return grounded;
    }

    private Collider2D GroundRay(Vector2 origin, float distance)
    {
        int count = Physics2D.Raycast(origin, Vector2.down, groundFilter, groundHits, distance);

        for(int i = 0; i < count; i++)
        {
            Collider2D hitCollider = groundHits[i].collider;

            if(hitCollider == null || hitCollider.attachedRigidbody == rb)
                continue;

            if (shadowEquipped && hitCollider.CompareTag("Platforms") &&
                playerCollider.bounds.min.y < hitCollider.bounds.max.y - 0.04f)
                continue;
            if(hitCollider.CompareTag("Floor") || hitCollider.CompareTag("Platforms") ||
               hitCollider.GetComponent<CyclopsCompanion>() != null)
                return hitCollider;
        }

        return null;
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if(isDead || isWallBouncing) return;

        if(other.gameObject.CompareTag("Wall1"))
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            rb.AddForce(Vector2.right * wallBounce, ForceMode2D.Impulse);
            isWallBouncing = true;
            StartCoroutine(WallBounceTimer());
        }
        else if(other.gameObject.CompareTag("Wall2"))
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            rb.AddForce(Vector2.left * wallBounce, ForceMode2D.Impulse);
            isWallBouncing = true;
            StartCoroutine(WallBounceTimer());
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if(other.CompareTag("Sector"))
        {
            sectorsInside++;
            inSector = true;
        }

        if(!isDead && (other.CompareTag("Enemy") || (manager != null && manager.IsLaser(other))))
        {
            TuffyController tuffy = other.GetComponentInParent<TuffyController>();
            if (tuffy != null && lokiEquipped && Time.time >= lokiParryReady)
            {
                lokiParryReady = Time.time + 3f;
                tuffy.Slow(0.9f);
                tuffy.Repel(15f, false);
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
                rb.AddForce(Vector2.up * (naturalBounceForce + bounceforce) * jumpForceMultiplier, ForceMode2D.Impulse);
                return;
            }
            if(tuffy != null && patrickTotemEquipped && !patrickTotemUsed)
            {
                ParryTuffy(tuffy);
                return;
            }

            isDead = true;

            if(manager != null)
            {
                if (manager.IsLaser(other)) manager.PlayLaserImpactSound();
                manager.GameOver();
            }
            else
            {
                Debug.LogWarning("Asigna GameManager en PlayerController.", this);
            }

            Destroy(this);
        }
    }

    private void ParryTuffy(TuffyController tuffy)
    {
        patrickTotemUsed = true;
        DashPower dash = GetComponent<DashPower>();
        if (dash != null) dash.CancelForBounce();
        tuffy.Repel(50f);
        if (manager != null && manager.platformPrefab != null && manager.platformContainer != null)
        {
            BoxCollider2D sourceCollider = manager.platformPrefab.GetComponent<BoxCollider2D>();
            float width = sourceCollider != null
                ? sourceCollider.size.x * Mathf.Abs(manager.platformPrefab.transform.localScale.x) : 3f;
            float y = playerCollider.bounds.min.y - 1.5f;
            for (int i = -1; i <= 1; i++)
            {
                GameObject platform = Instantiate(manager.platformPrefab,
                    new Vector3(transform.position.x + i * width, y, 0f), Quaternion.identity,
                    manager.platformContainer.transform);
                StoreProgress.ConfigureShadowPlatform(platform.GetComponent<Collider2D>());
            }
        }
        extraJumpsRemaining = extraJumpsPerLanding;
        marineeAirJumpsUsed = 0;
        jumpUsedSinceLanding = true;
        jumpBufferCounter = 0f;
        coyoteTimeCounter = 0f;
        isGrounded = false;
        alreadyJumped = true;

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.up * (naturalBounceForce + bounceforce) * jumpForceMultiplier,
                    ForceMode2D.Impulse);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if(other.CompareTag("Sector"))
        {
            sectorsInside = Mathf.Max(0, sectorsInside - 1);
            inSector = sectorsInside > 0;
        }
    }

    private IEnumerator WallBounceTimer()
    {
        yield return new WaitForSeconds(0.2f);
        isWallBouncing = false;
    }

    private void OnDrawGizmosSelected()
    {
        Collider2D col = GetComponent<Collider2D>();
        if(col == null) return;

        Bounds bounds = col.bounds;
        float inset = Mathf.Min(0.08f, bounds.extents.x * 0.5f);
        float startY = bounds.min.y + 0.04f;
        float distance = groundCheckDistance + 0.04f;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(new Vector3(bounds.min.x + inset, startY, 0f),
                        new Vector3(bounds.min.x + inset, startY - distance, 0f));
        Gizmos.DrawLine(new Vector3(bounds.center.x, startY, 0f),
                        new Vector3(bounds.center.x, startY - distance, 0f));
        Gizmos.DrawLine(new Vector3(bounds.max.x - inset, startY, 0f),
                        new Vector3(bounds.max.x - inset, startY - distance, 0f));
    }
}

public class HamburgerPower : MonoBehaviour
{
    public bool IsFlying => flying;
    private Rigidbody2D body;
    private PlayerController player;
    private Sprite icon;
    private float flightUntil;
    private float readyAt;
    private float previousGravity;
    private bool flying;
    private RectTransform hud;
    private UnityEngine.UI.Image ring;
    private TMPro.TMP_Text label;
    private Sprite ringSprite;
    private SpriteRenderer playerSprite;
    private SpriteRenderer flightGlow;
    private SpriteRenderer flightSprite;
    private Transform flightVisual;
    private bool playerSpriteWasEnabled;
    private Material flightMaterial;
    private ParticleSystem flightTrail;

    public void Initialize(Sprite sprite) { icon = sprite; }
    private void Awake() { body = GetComponent<Rigidbody2D>(); player = GetComponent<PlayerController>(); playerSprite = GetComponent<SpriteRenderer>(); }
    private void Start()
    {
        TMPro.TMP_Text coins = GameObject.Find("Coin Text").GetComponent<TMPro.TMP_Text>();
        var root = new GameObject("Hamburger Cooldown", typeof(RectTransform));
        hud = root.GetComponent<RectTransform>();
        hud.SetParent(coins.GetComponentInParent<Canvas>().transform, false);
        hud.anchorMin = hud.anchorMax = new Vector2(1f, 0f);
        hud.pivot = new Vector2(1f, 0f);
        int index = StoreProgress.ActiveHudIndex(StoreProgress.Hamburger);
        hud.anchoredPosition = new Vector2(-28f, 138f - 110f * index);
        hud.sizeDelta = new Vector2(100f, 112f);
        if (MobileControls.IsMobile) MobileControls.ConfigureActiveHud(hud, index);
        ringSprite = CheesePower.CreatePixelRing();
        CheesePower.CreateImage(hud, "Ring Back", new Vector2(0f, 12f), new Vector2(82f, 82f), ringSprite, new Color(.15f,.24f,.34f,.9f));
        ring = CheesePower.CreateImage(hud, "Recharge", new Vector2(0f, 12f), new Vector2(82f, 82f), ringSprite, new Color(1f,.86f,.42f));
        ring.type = UnityEngine.UI.Image.Type.Filled;
        ring.fillMethod = UnityEngine.UI.Image.FillMethod.Radial360;
        ring.fillOrigin = (int)UnityEngine.UI.Image.Origin360.Top;
        var image = CheesePower.CreateImage(hud, "Hamburger", new Vector2(0f, 12f), new Vector2(52f, 52f), icon, Color.white);
        image.preserveAspect = true;
        UiSpriteAnimation.Attach(image, icon);
        CheesePower.CreateLabel(hud, MobileControls.IsMobile ? "TAP" : StoreProgress.UsesSecondaryActiveKey(StoreProgress.Hamburger) ? "SHIFT" : "E", new Vector2(32f,45f), new Vector2(35f,24f), coins.font, 14f, Color.white);
        label = CheesePower.CreateLabel(hud, "READY", new Vector2(0f,-37f), new Vector2(95f,22f), coins.font, 14f, Color.white);
        CreateFlightEffect();
    }
    private void Update()
    {
        if (player == null || player.manager == null || player.manager.isGameOver) { EndFlight(); return; }
        if (Time.timeScale <= 0f) return;
        if (flying && Time.time >= flightUntil) EndFlight();
        float remaining = Mathf.Max(0f, readyAt - Time.time);
        ring.fillAmount = flying ? 1f : 1f - Mathf.Clamp01(remaining / 16f);
        label.text = flying ? "FLY!" : remaining > 0f ? Mathf.CeilToInt(remaining) + "s" : "READY";
        Keyboard keyboard = Keyboard.current;
        bool pressed = MobileControls.ConsumeActive(StoreProgress.ActiveHudIndex(StoreProgress.Hamburger)) ||
            keyboard != null && (StoreProgress.UsesSecondaryActiveKey(StoreProgress.Hamburger)
                ? keyboard.leftShiftKey.wasPressedThisFrame || keyboard.rightShiftKey.wasPressedThisFrame
                : keyboard.eKey.wasPressedThisFrame);
        if (pressed && !flying && remaining <= 0f)
        {
            previousGravity = body.gravityScale;
            flying = true;
            flightUntil = Time.time + 4f;
            if (flightSprite != null)
            {
                playerSpriteWasEnabled = playerSprite.enabled;
                flightSprite.sprite = playerSprite.sprite;
                flightSprite.enabled = playerSpriteWasEnabled;
                playerSprite.enabled = false;
            }
            if (flightGlow != null) flightGlow.enabled = playerSpriteWasEnabled;
            if (flightTrail != null) flightTrail.Play();
        }
    }
    private void FixedUpdate()
    {
        if (!flying) return;
        body.gravityScale = 0f;
        float flightSpeed = player.LokiJumpImpulse / Mathf.Max(0.01f, body.mass);
        body.linearVelocity = new Vector2(body.linearVelocity.x, flightSpeed);
    }
    private void LateUpdate()
    {
        if (!flying || playerSprite == null) return;
        if (flightVisual != null) flightVisual.Rotate(0f, 0f, -720f * Time.deltaTime, Space.Self);
        if (flightSprite != null)
        {
            flightSprite.sprite = playerSprite.sprite;
            flightSprite.flipX = playerSprite.flipX;
            flightSprite.flipY = playerSprite.flipY;
            flightSprite.color = playerSprite.color;
        }
        if (flightGlow != null)
        {
            flightGlow.sprite = playerSprite.sprite;
            flightGlow.flipX = playerSprite.flipX;
            flightGlow.flipY = playerSprite.flipY;
            flightGlow.color = new Color(1f, 1f, 1f, 0.72f + 0.22f * Mathf.Sin(Time.time * 15f));
        }
    }

    private void CreateFlightEffect()
    {
        if (playerSprite == null) return;
        GameObject visual = new GameObject("Hamburger Flight Visual", typeof(SpriteRenderer));
        visual.transform.SetParent(transform, false);
        flightVisual = visual.transform;
        flightSprite = visual.GetComponent<SpriteRenderer>();
        flightSprite.sharedMaterial = playerSprite.sharedMaterial;
        flightSprite.sortingLayerID = playerSprite.sortingLayerID;
        flightSprite.sortingOrder = playerSprite.sortingOrder;
        flightSprite.enabled = false;
        Shader shader = Shader.Find("SkyMarbles/DashFlash");
        if (shader != null)
        {
            flightMaterial = new Material(shader);
            flightMaterial.SetFloat("_Flash", 1f);
            GameObject glow = new GameObject("Hamburger Flight Glow", typeof(SpriteRenderer));
            glow.transform.SetParent(flightVisual, false);
            flightGlow = glow.GetComponent<SpriteRenderer>();
            flightGlow.sharedMaterial = flightMaterial;
            flightGlow.sortingLayerID = playerSprite.sortingLayerID;
            flightGlow.sortingOrder = playerSprite.sortingOrder + 1;
            flightGlow.enabled = false;
        }

        GameObject trail = new GameObject("Hamburger Flight Trail");
        trail.transform.SetParent(transform, false);
        flightTrail = trail.AddComponent<ParticleSystem>();
        flightTrail.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = flightTrail.main;
        main.playOnAwake = false;
        main.loop = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.38f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.65f, 0.85f, 1f, 0.85f), Color.white);
        var emission = flightTrail.emission;
        emission.rateOverTime = 65f;
        var shape = flightTrail.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 18f;
        shape.radius = 0.22f;
        shape.rotation = new Vector3(90f, 0f, 0f);
        var colors = flightTrail.colorOverLifetime;
        colors.enabled = true;
        Gradient fade = new Gradient();
        fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(0.5f, 0.75f, 1f), 1f) },
            new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0f, 1f) });
        colors.color = fade;
        ParticleSystemRenderer trailRenderer = flightTrail.GetComponent<ParticleSystemRenderer>();
        trailRenderer.sortingLayerID = playerSprite.sortingLayerID;
        trailRenderer.sortingOrder = playerSprite.sortingOrder - 1;
    }
    private void EndFlight()
    {
        if (!flying) return;
        flying = false;
        if (flightSprite != null)
        {
            flightSprite.enabled = false;
            playerSprite.enabled = playerSpriteWasEnabled;
            flightVisual.localRotation = Quaternion.identity;
        }
        if (flightGlow != null) flightGlow.enabled = false;
        if (flightTrail != null) flightTrail.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        body.gravityScale = previousGravity;
        readyAt = Time.time + 16f;
    }
    private void OnDisable() { EndFlight(); }
    private void OnDestroy()
    {
        if (hud != null) Destroy(hud.gameObject);
        if (flightMaterial != null) Destroy(flightMaterial);
        if (ringSprite != null) { Destroy(ringSprite.texture); Destroy(ringSprite); }
    }
}

public class LokiSparkle : MonoBehaviour
{
    private ParticleSystem particles;
    private ParticleSystem.Particle[] buffer;
    private bool slowed;
    public static void Play(Vector3 position)
    {
        GameObject effect = new GameObject("Loki sparkles");
        effect.transform.position = position;
        effect.AddComponent<LokiSparkle>().Initialize();
        Destroy(effect, 1.5f);
    }
    private void Initialize()
    {
        particles = gameObject.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = particles.main;
        main.duration = .2f; main.loop = false; main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = 1f; main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 3f);
        main.startSize = new ParticleSystem.MinMaxCurve(.08f, .18f);
        var emission = particles.emission; emission.rateOverTime = 0; emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 24) });
        var shape = particles.shape; shape.shapeType = ParticleSystemShapeType.Circle; shape.radius = .3f;
        var color = particles.colorOverLifetime; color.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(new[] { new GradientColorKey(new Color(.55f,.1f,.9f), 0f), new GradientColorKey(Color.red, .33f), new GradientColorKey(Color.yellow, .66f), new GradientColorKey(new Color(1f,.3f,.65f), 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        color.color = gradient;
        buffer = new ParticleSystem.Particle[32];
        particles.Play();
    }
    private void Update()
    {
        if (slowed || particles == null) return;
        TuffyController tuffy = FindAnyObjectByType<TuffyController>();
        if (tuffy == null) return;
        Collider2D collider = tuffy.GetComponentInChildren<Collider2D>();
        if (collider == null) return;
        int count = particles.GetParticles(buffer);
        for (int i = 0; i < count; i++)
        {
            Vector2 point = buffer[i].position;
            if (((Vector2)collider.ClosestPoint(point) - point).sqrMagnitude > .01f) continue;
            tuffy.Slow(.9f);
            slowed = true;
            break;
        }
    }
}
