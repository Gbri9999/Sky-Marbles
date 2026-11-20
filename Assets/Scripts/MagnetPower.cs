using System.Collections.Generic;
using UnityEngine;

public class MagnetPower : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float radius = 3.5f;
    [SerializeField, Min(0.1f)] private float pullSpeed = 22f;

    private readonly List<Collider2D> nearby = new List<Collider2D>();
    private ContactFilter2D coinFilter;
    private bool equipped;
    private bool aspiratorEquipped;

    private void Awake()
    {
        equipped = StoreProgress.IsEquipped(StoreProgress.Magnet);
        aspiratorEquipped = StoreProgress.IsEquipped(StoreProgress.Aspirator);
        coinFilter = new ContactFilter2D();
        coinFilter.useTriggers = true;
    }

    private void FixedUpdate()
    {
        if (!equipped && !aspiratorEquipped) return;

        // The map is less than 10 units wide; this band covers it from either wall.
        int count = aspiratorEquipped
            ? Physics2D.OverlapBox(transform.position, new Vector2(100f, radius * 2.4f), 0f, coinFilter, nearby)
            : Physics2D.OverlapCircle(transform.position, radius, coinFilter, nearby);

        for (int i = 0; i < count; i++)
        {
            Coin coin = nearby[i].GetComponent<Coin>();
            if (coin == null) continue;

            coin.transform.position = Vector2.MoveTowards(
                coin.transform.position,
                transform.position,
                pullSpeed * Time.fixedDeltaTime
            );
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
