using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class CyclopsCompanion : MonoBehaviour
{
    private Transform player;
    private Rigidbody2D body;
    private Vector2 previousPlayer;
    private Vector2 velocity;
    private float startedMovingAt = -1f;
    private float offset;
    private float delay;
    private bool following;

    public void Initialize(Transform followedPlayer, float followDelay, float verticalOffset)
    {
        player = followedPlayer;
        body = GetComponent<Rigidbody2D>();
        previousPlayer = player.position;
        offset = verticalOffset;
        delay = followDelay;
    }

    private void FixedUpdate()
    {
        if (player == null) return;
        Vector2 current = player.position;
        bool moving = (current - previousPlayer).sqrMagnitude > 0.0001f;
        previousPlayer = current;
        if (!following && moving)
        {
            if (startedMovingAt < 0f) startedMovingAt = Time.time;
            if (Time.time - startedMovingAt >= delay) following = true;
        }
        if (!following && !moving) startedMovingAt = -1f;
        if (!following) return;
        Vector2 target = current + Vector2.down * offset;
        body.MovePosition(Vector2.SmoothDamp(body.position, target, ref velocity, 0.4f, Mathf.Infinity, Time.fixedDeltaTime));
    }
}
