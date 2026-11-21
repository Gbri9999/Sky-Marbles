using UnityEngine;

public class PufferFish : MonoBehaviour
{
    private Vector3 origin;
    private float phase;
    private float dip;
    private float nextBounce;
    private GameManager manager;
    private Collider2D hitbox;

    public static void Create(Vector3 position, Transform parent, RuntimeAnimatorController controller,
        GameManager gameManager, float startPhase)
    {
        GameObject fish = new GameObject("Puffer Fish");
        fish.transform.SetParent(parent, false);
        fish.transform.position = position;
        SpriteRenderer renderer = fish.AddComponent<SpriteRenderer>();
        renderer.sortingOrder = 3;
        Animator animator = fish.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        CircleCollider2D collider = fish.AddComponent<CircleCollider2D>();
        collider.radius = 0.65f;
        collider.isTrigger = true;
        PufferFish behavior = fish.AddComponent<PufferFish>();
        behavior.origin = position;
        behavior.phase = startPhase;
        behavior.manager = gameManager;
        behavior.hitbox = collider;
    }

    private void Update()
    {
        phase += Time.deltaTime * 1.1f;
        dip = Mathf.MoveTowards(dip, 0f, Time.deltaTime * 0.7f);
        transform.position = origin + new Vector3(Mathf.Cos(phase) * 0.55f,
            Mathf.Sin(phase) * 0.3f - dip, 0f);
        if (manager != null && manager.player != null && transform.position.y < manager.player.position.y - 35f)
            Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other) => TryBounce(other);
    private void OnTriggerStay2D(Collider2D other) => TryBounce(other);

    private void TryBounce(Collider2D other)
    {
        if (Time.time < nextBounce || manager == null || manager.isGameOver || !other.CompareTag("Player")) return;
        Rigidbody2D body = other.attachedRigidbody;
        PlayerController player = other.GetComponent<PlayerController>();
        if (body == null || player == null || body.linearVelocity.y >= 0f) return;
        // The player can enter from below and start falling while still overlapping.
        // Accept a descending contact on the upper half even if the top was crossed earlier.
        if (other.bounds.center.y <= hitbox.bounds.center.y) return;
        nextBounce = Time.time + 0.5f;
        dip = 0.3f;
        player.BounceFromPufferFish();
        manager.AddCoin();
    }
}
