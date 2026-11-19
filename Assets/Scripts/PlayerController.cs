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
    private float normalGravityScale;
    private bool patrickTotemEquipped;
    private bool patrickTotemUsed;
    private float previousBottomY;

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

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        normalGravityScale = rb.gravityScale;
        playerCollider = GetComponent<Collider2D>();
        previousBottomY = playerCollider.bounds.min.y;
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

    void FixedUpdate()
    {
        if(isDead || dashMovementActive) return;
        previousBottomY = playerCollider.bounds.min.y;
        
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
            rb.AddForce(Vector2.up * bounceforce * jumpForceMultiplier, ForceMode2D.Impulse);
            if (manager != null) manager.PlayJumpFeedback();
            alreadyJumped = true;
            extraJumpsRemaining--;
            jumpBufferCounter = 0f;
        }

        jumpBufferCounter = Mathf.Max(0f, jumpBufferCounter - Time.fixedDeltaTime);
        if (voltyEquipped)
            rb.gravityScale = !isGrounded && rb.linearVelocity.y < -0.01f
                ? normalGravityScale * 0.5f : normalGravityScale;
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
            if(tuffy != null && patrickTotemEquipped && !patrickTotemUsed &&
               rb.linearVelocity.y < 0f && previousBottomY >= other.bounds.max.y - 0.1f)
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
        tuffy.ResetSpeed();
        extraJumpsRemaining = extraJumpsPerLanding;
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
