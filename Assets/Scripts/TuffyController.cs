using UnityEngine;

public class TuffyController : MonoBehaviour
{
    public PlayerController controller;

    public float speed;
    public float valueToSum;
    private float initialSpeed;

    private void Awake()
    {
        initialSpeed = speed;
    }

    public void ResetSpeed()
    {
        speed = initialSpeed;
    }

    void Update()
    {
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
