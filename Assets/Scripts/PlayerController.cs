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

    [Header("Jump Assistance")]
    public float jumpBufferTime = 0.15f;
    public float coyoteTime = 0.12f;
    public bool holdSpaceForJump = true;

    [Header("Marble Powers")]
    [SerializeField, Range(0.2f, 0.9f)] private float breezeGravityMultiplier = 0.65f;

    [Header("Ground Detection")]
    public float groundCheckDistance = 0.12f;

    private float jumpBufferCounter;
    private float coyoteTimeCounter;
    private bool jumpUsedSinceLanding;
    private bool holdingSpace;
    private int extraJumpsPerLanding;
    private int extraJumpsRemaining;
    private float jumpForceMultiplier = 1f;
    private bool patrickTotemEquipped;
    private bool patrickTotemUsed;
    private float previousBottomY;

    private ContactFilter2D groundFilter;
    private readonly RaycastHit2D[] groundHits = new RaycastHit2D[12];

    public bool inSector;
    private int sectorsInside;

    public bool alreadyJumped;
    private bool isDead;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerCollider = GetComponent<Collider2D>();
        previousBottomY = playerCollider.bounds.min.y;
        patrickTotemEquipped = StoreProgress.IsEquipped(StoreProgress.PatrickTotem);
        string marble = PlayerPrefs.GetString(StoreProgress.SelectedCharacter, StoreProgress.Bluey);
        extraJumpsPerLanding = marble == StoreProgress.Bouncy && StoreProgress.IsOwned(StoreProgress.Bouncy) ? 2 : 0;
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

        if(keyboard.aKey.isPressed)
        {
            direction = -1;
        }
        else if(keyboard.dKey.isPressed)
        {
            direction = 1;
        }
        else direction = 0;

        holdingSpace = keyboard.spaceKey.isPressed;
        
        if(keyboard.spaceKey.wasPressedThisFrame)
        {
            jumpBufferCounter = jumpBufferTime;
        }
    }

    void FixedUpdate()
    {
        if(isDead) return;
        previousBottomY = playerCollider.bounds.min.y;
        
        if(!isWallBouncing)
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
        
        isGrounded = rb.linearVelocity.y <= 0.08f && CheckGround();

        if(isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
            
            if(!wasGrounded)
            {
                jumpUsedSinceLanding = false;
                extraJumpsRemaining = extraJumpsPerLanding;

                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
                rb.AddForce(Vector2.up * naturalBounceForce * jumpForceMultiplier, ForceMode2D.Impulse);
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
            rb.AddForce(Vector2.up * bounceforce * jumpForceMultiplier, ForceMode2D.Impulse);

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
            alreadyJumped = true;
            extraJumpsRemaining--;
            jumpBufferCounter = 0f;
        }

        jumpBufferCounter = Mathf.Max(0f, jumpBufferCounter - Time.fixedDeltaTime);
    }

    private bool CheckGround()
    {
        Bounds bounds = playerCollider.bounds;
        float inset = Mathf.Min(0.08f, bounds.extents.x * 0.5f);
        
        float startY = bounds.min.y + 0.04f;
        float distance = groundCheckDistance + 0.04f;

        Vector2 left = new Vector2(bounds.min.x + inset, startY);
        Vector2 center = new Vector2(bounds.center.x, startY);
        Vector2 right = new Vector2(bounds.max.x - inset, startY);

        return GroundRay(left, distance) ||
               GroundRay(center, distance) ||
               GroundRay(right, distance);
    }

    private bool GroundRay(Vector2 origin, float distance)
    {
        int count = Physics2D.Raycast(origin, Vector2.down, groundFilter, groundHits, distance);

        for(int i = 0; i < count; i++)
        {
            Collider2D hitCollider = groundHits[i].collider;

            if(hitCollider == null || hitCollider.attachedRigidbody == rb)
                continue;

            if(hitCollider.CompareTag("Floor") || hitCollider.CompareTag("Platforms"))
                return true;
        }

        return false;
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

        if(!isDead && other.CompareTag("Enemy"))
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
