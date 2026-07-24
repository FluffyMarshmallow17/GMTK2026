using UnityEngine;

// ONLY VIBE CODED SCRIPT, SORRY
public class Spin : MonoBehaviour 
{
    public float angularVelocity = 720f;
    public float damping = 4f;

    public float startScaleMultiplier = 1.3f;
    public float shrinkSpeed = 6f;

    private Vector3 targetScale;

    void Start()
    {
        targetScale = transform.localScale;
        transform.localScale = targetScale * startScaleMultiplier;
    }

    void Update()
    {
        // Rotate
        transform.Rotate(0, 0, angularVelocity * Time.deltaTime);
        angularVelocity *= Mathf.Exp(-damping * Time.deltaTime);

        if (Mathf.Abs(angularVelocity) < 1f)
            angularVelocity = 0f;

        // Shrink back to original size
        transform.localScale = Vector3.Lerp(
            transform.localScale,
            targetScale,
            shrinkSpeed * Time.deltaTime
        );
    }
}