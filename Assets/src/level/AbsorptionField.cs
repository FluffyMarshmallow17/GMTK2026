using UnityEngine;

public class AbsorptionField : MonoBehaviour {

    CircleCollider2D circleCollider;

    void Awake()
    {
        circleCollider = GetComponent<CircleCollider2D>();
    }

    void OnTriggerStay2D(Collider2D other)
    {
        Rigidbody2D rb = other.GetComponent<Rigidbody2D>();
        float colliderRadius = circleCollider.radius;

        if (rb != null)
        {
            if (other.CompareTag("Block")) {
                Vector2 direction = transform.position - other.transform.position;
                float strength = 1 / direction.magnitude;
                rb.AddForce(direction.normalized * 50 * strength);

                float t = Mathf.Clamp01(direction.magnitude / colliderRadius);
                float scale = Mathf.Lerp(0.1f, 1f, t);
                other.transform.localScale = Vector3.one * scale;
            }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Rigidbody2D rb = other.GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            if (other.CompareTag("Block"))
            {
                rb.AddTorque(3);
            }
        }

    }
}