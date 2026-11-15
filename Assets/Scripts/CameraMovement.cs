
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    public Transform player;
    public PlayerController controller;

    public float smoothTimeUp = 0.3f;
    public float smoothTimeDown = 0.25f;

    public float upThreshold = 1.5f;
    public float downThreshold = 2f;

    private float cameraVelocity;
    private float targetY;

    void Start()
    {
        targetY = transform.position.y;
    }

    void LateUpdate()
    {
        if (controller.inSector)
        {
            cameraVelocity = 0f;
            targetY = transform.position.y;
            return;
        }

        float playerY = player.position.y;
        float cameraY = transform.position.y;

        if (playerY > cameraY + upThreshold)
        {
            targetY = playerY - upThreshold;
        }
        else if (playerY < cameraY - downThreshold)
        {
            targetY = playerY + downThreshold;
        }

        float currentSmoothTime = targetY < cameraY
            ? smoothTimeDown
            : smoothTimeUp;

        float newPosition = Mathf.SmoothDamp(
            cameraY,
            targetY,
            ref cameraVelocity,
            currentSmoothTime
        );

        transform.position = new Vector3(
            transform.position.x,
            newPosition,
            transform.position.z
        );
    }
}
