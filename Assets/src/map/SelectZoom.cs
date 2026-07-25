using UnityEngine;

// Zooms the selection marker onto a block: starts large, then decelerates into place.
public class SelectZoom : MonoBehaviour
{
    public float startScaleMultiplier = 2.8f;
    [Tooltip("Time to settle onto the target scale. Lower = snappier zoom-in.")]
    public float scaleSmoothTime = 0.08f;

    Vector3 targetScale;
    float scaleVelocity;

    void Start()
    {
        targetScale = transform.localScale;
        transform.localScale = targetScale * startScaleMultiplier;
        // Strong inward velocity so the zoom begins fast, then SmoothDamp eases out.
        scaleVelocity = -Mathf.Abs(targetScale.x * (startScaleMultiplier - 1f) / Mathf.Max(0.01f, scaleSmoothTime));
    }

    void Update()
    {
        float next = Mathf.SmoothDamp(
            transform.localScale.x,
            targetScale.x,
            ref scaleVelocity,
            scaleSmoothTime,
            Mathf.Infinity,
            Time.deltaTime);
        transform.localScale = new Vector3(next, next, targetScale.z);
    }
}
