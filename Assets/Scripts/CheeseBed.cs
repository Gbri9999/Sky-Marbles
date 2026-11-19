using UnityEngine;

public class CheeseBed : MonoBehaviour
{
    private float expiresAt;
    private Vector3 restingScale;
    private bool consumed;

    public void Initialize(float lifetime)
    {
        expiresAt = Time.time + lifetime;
        restingScale = transform.localScale;
    }

    private void Update()
    {
        if (consumed) return;
        if (Time.time >= expiresAt)
        {
            Destroy(gameObject);
            return;
        }

        float pulse = 1f + 0.035f * Mathf.Sin(Time.time * 10f);
        transform.localScale = new Vector3(restingScale.x, restingScale.y * pulse, restingScale.z);
    }

    public void Consume()
    {
        if (consumed) return;
        consumed = true;
        GetComponent<Collider2D>().enabled = false;
        Destroy(gameObject, 0.28f);
    }
}
