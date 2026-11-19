using UnityEngine;

public class PlatformFracture : MonoBehaviour
{
    private const int pieceCount = 3;
    private const float duration = 0.65f;

    private readonly SpriteRenderer[] pieces = new SpriteRenderer[pieceCount];
    private readonly Sprite[] pieceSprites = new Sprite[pieceCount];
    private readonly Vector2[] velocities = new Vector2[pieceCount];
    private readonly float[] spinSpeeds = new float[pieceCount];
    private float sourceAlpha;
    private float elapsed;

    public static void Play(GameObject platform, System.Random random)
    {
        SpriteRenderer source = null;
        foreach (SpriteRenderer renderer in platform.GetComponentsInChildren<SpriteRenderer>())
        {
            if (!renderer.enabled || renderer.sprite == null) continue;
            source = renderer;
            break;
        }

        if (source == null) return;

        GameObject effect = new GameObject("Platform Fragments");
        effect.AddComponent<PlatformFracture>().CreatePieces(source, random);
    }

    private void CreatePieces(SpriteRenderer source, System.Random random)
    {
        Rect sourceRect = source.sprite.textureRect;
        float pieceWidth = sourceRect.width / pieceCount;
        float worldWidth = source.bounds.size.x / pieceCount;
        Vector3 center = source.bounds.center;
        sourceAlpha = source.color.a;

        for (int i = 0; i < pieceCount; i++)
        {
            Rect rect = new Rect(sourceRect.x + pieceWidth * i, sourceRect.y,
                pieceWidth, sourceRect.height);
            pieceSprites[i] = Sprite.Create(source.sprite.texture, rect,
                new Vector2(0.5f, 0.5f), source.sprite.pixelsPerUnit);

            GameObject piece = new GameObject("Fragment " + (i + 1));
            piece.transform.SetParent(transform, false);
            piece.transform.position = center + Vector3.right * ((i - 1) * worldWidth);
            piece.transform.rotation = source.transform.rotation;
            piece.transform.localScale = source.transform.lossyScale;

            SpriteRenderer renderer = piece.AddComponent<SpriteRenderer>();
            renderer.sprite = pieceSprites[i];
            renderer.sharedMaterial = source.sharedMaterial;
            renderer.sortingLayerID = source.sortingLayerID;
            renderer.sortingOrder = source.sortingOrder;
            renderer.color = source.color;
            pieces[i] = renderer;

            float spread = (i - 1) * 1.7f + Mathf.Lerp(-0.35f, 0.35f,
                (float)random.NextDouble());
            velocities[i] = new Vector2(spread,
                Mathf.Lerp(1.4f, 2.5f, (float)random.NextDouble()));
            spinSpeeds[i] = (i == 1 ? 1f : i - 1) *
                Mathf.Lerp(90f, 190f, (float)random.NextDouble());
        }
    }

    private void Update()
    {
        float delta = Time.deltaTime;
        elapsed += delta;
        float fade = 1f - Mathf.SmoothStep(0f, 1f,
            Mathf.Clamp01((elapsed - 0.18f) / (duration - 0.18f)));

        for (int i = 0; i < pieces.Length; i++)
        {
            velocities[i] += Vector2.down * (8f * delta);
            Transform fragment = pieces[i].transform;
            fragment.position += (Vector3)(velocities[i] * delta);
            fragment.Rotate(0f, 0f, spinSpeeds[i] * delta);

            Color color = pieces[i].color;
            color.a = sourceAlpha * fade;
            pieces[i].color = color;
        }

        if (elapsed >= duration) Destroy(gameObject);
    }

    private void OnDestroy()
    {
        foreach (Sprite sprite in pieceSprites)
            if (sprite != null) Destroy(sprite);
    }
}
