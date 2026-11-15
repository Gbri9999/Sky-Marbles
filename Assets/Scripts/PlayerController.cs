using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    private Rigidbody2D rb;
    public GameManager manager;

    private bool isGrounded;

    public float naturalBounceForce;
    public float bounceforce;
    public float wallBounce;

    private int direction;
    public float velocity;
    private float newVelocity;
    private float smoothTime;
    private float velocityRef;
    private bool isWallBouncing;

    public bool inSector;

    public bool alreadyJumped;
   
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

   
    void Update()
    {
        newVelocity = Mathf.SmoothDamp(newVelocity, velocity * direction, ref velocityRef, smoothTime);

        if(Keyboard.current.aKey.isPressed)
        {
            direction = -1;
        }
        else if(Keyboard.current.dKey.isPressed)
        {
            direction = 1;
        }
        else direction = 0;

        if(!isWallBouncing)
        {
            rb.linearVelocity = new Vector2(newVelocity, rb.linearVelocity.y);
        }

        if(isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            rb.AddForce(Vector2.up * naturalBounceForce, ForceMode2D.Impulse);

            if(Keyboard.current.spaceKey.isPressed)
            {
                rb.AddForce(Vector2.up * bounceforce, ForceMode2D.Impulse);

                alreadyJumped = true;
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if(other.gameObject.CompareTag("Floor"))
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            isGrounded = true;
        }

        if(other.gameObject.CompareTag("Platforms"))
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            isGrounded = true;
        }

        if(other.gameObject.CompareTag("Wall1"))
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            rb.AddForce(Vector2.right * wallBounce, ForceMode2D.Impulse);
            StartCoroutine(WallBounceTimer());
        }

        if(other.gameObject.CompareTag("Wall2"))
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            rb.AddForce(Vector2.left * wallBounce, ForceMode2D.Impulse);
            StartCoroutine(WallBounceTimer());
        }
    }

    private void OnCollisionExit2D(Collision2D other)
    {
        if(other.gameObject.CompareTag("Floor"))
        {
            isGrounded = false;
        }

        if(other.gameObject.CompareTag("Platforms"))
        {
            isGrounded = false;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if(other.CompareTag("Sector"))
        {
            inSector = true;
        }
        
        if(other.gameObject.CompareTag("Enemy"))
        {
            manager.GameOver();
            Destroy(this);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if(other.CompareTag("Sector"))
        {
            inSector = false;
        }
    }
    private IEnumerator WallBounceTimer()
    {
        isWallBouncing = true;

        yield return new WaitForSeconds(0.2f);

        isWallBouncing = false;
    }
}
