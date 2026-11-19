using UnityEngine;
using System.Collections;

public class CyclopsPower : MonoBehaviour
{
    [SerializeField] private Sprite cyclopsSprite;
    [SerializeField] private RuntimeAnimatorController cyclopsAnimation;
    [SerializeField, Min(0.1f)] private float followDelay = 2f;
    [SerializeField, Min(0.5f)] private float verticalOffset = 1.6f;

    private GameObject companion;

    private IEnumerator Start()
    {
        if (!StoreProgress.IsEquipped(StoreProgress.Cyclops) ||
            cyclopsSprite == null || cyclopsAnimation == null) yield break;

        PlayerController player = GetComponent<PlayerController>();
        if (player == null) yield break;
        yield return new WaitUntil(() => player.alreadyJumped);

        companion = new GameObject("Cyclops Companion");
        companion.transform.localScale = Vector3.one * 2f;
        companion.transform.position = transform.position + Vector3.down * verticalOffset;
        SpriteRenderer renderer = companion.AddComponent<SpriteRenderer>();
        renderer.sprite = cyclopsSprite;
        renderer.sortingOrder = 3;
        renderer.color = new Color(1f, 1f, 1f, 0f);
        Animator animator = companion.AddComponent<Animator>();
        animator.runtimeAnimatorController = cyclopsAnimation;

        Rigidbody2D body = companion.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        BoxCollider2D platform = companion.AddComponent<BoxCollider2D>();
        platform.size = new Vector2(0.78f, 0.2f);
        platform.offset = new Vector2(0f, 0.36f);
        platform.usedByEffector = true;
        PlatformEffector2D oneWay = companion.AddComponent<PlatformEffector2D>();
        oneWay.useOneWay = true;
        oneWay.surfaceArc = 120f;

        companion.AddComponent<CyclopsCompanion>().Initialize(transform, followDelay, verticalOffset);
        float elapsed = 0f;
        while (elapsed < 0.4f && renderer != null)
        {
            elapsed += Time.deltaTime;
            renderer.color = new Color(1f, 1f, 1f, Mathf.Clamp01(elapsed / 0.4f));
            yield return null;
        }
    }

    private void OnDestroy()
    {
        if (companion != null) Destroy(companion);
    }
}
