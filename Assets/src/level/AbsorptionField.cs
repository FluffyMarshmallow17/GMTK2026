using UnityEngine;

public class AbsorptionField : MonoBehaviour {

    void OnTriggerStay2D(Collider2D other)
    {
        Rigidbody2D rb = other.GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            Vector2 direction = transform.position - other.transform.position;
            float strength = 1 / direction.magnitude;
            rb.AddForce(direction.normalized * 50 * strength);
        }
    }
}