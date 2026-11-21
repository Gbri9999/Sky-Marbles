using UnityEngine;

public class TuffyController : MonoBehaviour
{
    public PlayerController controller;

    public float speed;
    public float valueToSum;
    private float initialSpeed;
    private float repelRemaining;

    private void Awake()
    {
        initialSpeed = speed;
    }

    public void Slow(float multiplier)
    {
        speed *= Mathf.Clamp01(multiplier);
    }

    public void Repel(float distance, bool resetSpeed = true)
    {
        if (resetSpeed) speed = initialSpeed;
        repelRemaining = Mathf.Max(repelRemaining, distance);
    }

    void Update()
    {
        if (repelRemaining > 0f)
        {
            float step = Mathf.Min(repelRemaining, 25f * Time.deltaTime);
            transform.position += Vector3.down * step;
            repelRemaining -= step;
            return;
        }
        if(controller.alreadyJumped)
        {
            transform.position = new Vector3(transform.position.x, transform.position.y + speed * Time.deltaTime, transform.position.z);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if(other.CompareTag("Platforms") && other.GetComponent<CyclopsCompanion>() == null)
        {
            speed = speed + valueToSum;
        }
    }
}
