using UnityEngine;

public class MagnetPower : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float radius = 3.5f;
    [SerializeField, Min(0.1f)] private float pullSpeed = 8f;

    private readonly Collider2D[] nearby = new Collider2D[32];
    private ContactFilter2D coinFilter;
    private bool equipped;

    private void Awake()
    {
        equipped = StoreProgress.IsEquipped(StoreProgress.Magnet);
        coinFilter = new ContactFilter2D();
        coinFilter.useTriggers = true;
    }

    private void FixedUpdate()
    {
        if (!equipped) return;

        int count = Physics2D.OverlapCircle(transform.position, radius,
            coinFilter, nearby);

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
