using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    public Transform player;
    public PlayerController controller;

    public float smoothTimeUp = 0.3f;
    public float smoothTimeDown = 0.25f;

    public float upThreshold = 1.5f;
    public float downThreshold = 2f;

    [Header("Background by altitude")]
    [SerializeField, Min(1f)] private float heightToDarkestColor = 170f;
    [SerializeField] private Color darkestBackgroundColor = new Color(0.07f, 0.045f, 0.16f, 1f);

    private float cameraVelocity;
    private float targetY;
    private Vector2 shakeOffset;
    private float shakeEnd;
    private float shakeDuration;
    private float shakeStrength;
    private Camera cameraComponent;
    private Color startingBackgroundColor;
    private float startingPlayerHeight;
    private float highestPlayerHeight;

    void Start()
    {
        cameraComponent = GetComponent<Camera>();
        targetY = transform.position.y;
        startingBackgroundColor = cameraComponent.backgroundColor;
        startingPlayerHeight = player.position.y;
        highestPlayerHeight = startingPlayerHeight;
    }

    public void Shake(float duration, float strength)
    {
        shakeDuration = duration;
        shakeStrength = strength;
        shakeEnd = Time.time + duration;
    }

    public void SetMirror(bool active)
    {
        if (active)
            SetMirrorScale(-1f);
        else
            cameraComponent.ResetProjectionMatrix();
    }

    public void SetMirrorScale(float scale)
    {
        cameraComponent.ResetProjectionMatrix();
        float visibleScale = Mathf.Sign(scale) * Mathf.Max(0.03f, Mathf.Abs(scale));
        cameraComponent.projectionMatrix = Matrix4x4.Scale(new Vector3(visibleScale, 1f, 1f)) * cameraComponent.projectionMatrix;
    }

    void LateUpdate()
    {
        Vector3 position = transform.position - (Vector3)shakeOffset;
        float cameraY = position.y;

        if (controller != null && controller.inSector)
        {
            cameraVelocity = 0f;
            targetY = cameraY;
        }
        else
        {
            float playerY = player.position.y;
            if (playerY > cameraY + upThreshold)
                targetY = playerY - upThreshold;
            else if (playerY < cameraY - downThreshold)
                targetY = playerY + downThreshold;

            float currentSmoothTime = targetY < cameraY ? smoothTimeDown : smoothTimeUp;
            position.y = Mathf.SmoothDamp(cameraY, targetY, ref cameraVelocity, currentSmoothTime);
        }

        shakeOffset = Time.time < shakeEnd
            ? Random.insideUnitCircle * (shakeStrength * (shakeEnd - Time.time) / shakeDuration)
            : Vector2.zero;
        transform.position = position + (Vector3)shakeOffset;

        highestPlayerHeight = Mathf.Max(highestPlayerHeight, player.position.y);
        float darkness = Mathf.Clamp01((highestPlayerHeight - startingPlayerHeight) / heightToDarkestColor);
        Color targetColor = darkestBackgroundColor;
        targetColor.a = startingBackgroundColor.a;
        cameraComponent.backgroundColor = Color.Lerp(startingBackgroundColor, targetColor, darkness);
    }
}
