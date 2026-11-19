using UnityEngine;
using UnityEngine.UI;

public class UiSpriteAnimation : MonoBehaviour
{
    private Image image;
    private Sprite[] frames;
    public static void Attach(Image target, Sprite source)
    {
        if (target == null || source == null || source.texture == null) return;
        int width = Mathf.RoundToInt(source.rect.width);
        int height = Mathf.RoundToInt(source.rect.height);
        int count = source.texture.width / width;
        if (count <= 1 || width <= 0 || height <= 0) return;
        var animation = target.gameObject.AddComponent<UiSpriteAnimation>();
        animation.image = target;
        animation.frames = new Sprite[count];
        for (int i = 0; i < count; i++)
            animation.frames[i] = Sprite.Create(source.texture, new Rect(i * width, source.rect.y, width, height), new Vector2(0.5f, 0.5f), source.pixelsPerUnit);
    }
    private void Update()
    {
        if (frames != null) image.sprite = frames[(int)(Time.unscaledTime * 12f) % frames.Length];
    }
    private void OnDestroy()
    {
        if (frames != null) foreach (var frame in frames) Destroy(frame);
    }
}
