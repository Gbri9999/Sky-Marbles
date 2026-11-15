using UnityEngine;

public class TuffyController : MonoBehaviour
{
    public PlayerController controller;

    public float speed;
    public float valueToSum;

    void Update()
    {
        if(controller.alreadyJumped)
        {
            transform.position = new Vector3(transform.position.x, transform.position.y + speed * Time.deltaTime, transform.position.z);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if(other.CompareTag("Platforms"))
        {
            speed = speed + valueToSum;
        }
    }
}
