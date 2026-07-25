using TMPro;
using UnityEngine;

/// <summary>
/// Briefly hides the countdown label and flashes an operation sprite that pops in,
/// fits inside the host body, stays dark for glow contrast, then restores the number.
/// </summary>
public class OperationFlash
{
    public float startScaleMultiplier = 0.35f;
    public float peakScaleMultiplier = 1.12f;
    public float settleSmoothTime = 0.1f;
    public float holdDuration = 0.2f;
    [Tooltip("How much of the host body the flash should fill at rest (0-1).")]
    public float fitFraction = 0.55f;
    public Color flashColor = new Color(0.05f, 0.05f, 0.07f, 1f);

    TextMeshPro label;
    Transform host;
    SpriteRenderer hostBody;
    SpriteRenderer flashRenderer;
    Transform flashTransform;
    Vector3 restScale = Vector3.one * 0.4f;
    float scaleVelocity;
    float holdTimer;
    bool settling;
    bool active;

    public bool IsActive => active;

    public void Init(TextMeshPro countdownLabel, Transform hostTransform = null)
    {
        label = countdownLabel;
        host = hostTransform != null ? hostTransform : (countdownLabel != null ? countdownLabel.transform.root : null);
        if (label == null)
            return;

        if (host != null)
        {
            Transform spriteChild = host.Find("Sprite");
            if (spriteChild != null)
                hostBody = spriteChild.GetComponent<SpriteRenderer>();
        }

        GameObject flashObject = new GameObject("OperationFlash");
        flashTransform = flashObject.transform;
        flashTransform.SetParent(host != null ? host : label.transform.parent, false);
        flashTransform.localPosition = Vector3.zero;
        flashTransform.localRotation = Quaternion.identity;

        flashRenderer = flashObject.AddComponent<SpriteRenderer>();
        flashRenderer.color = flashColor;
        flashRenderer.sortingOrder = 80;
        flashRenderer.enabled = false;
    }

    public void Play(Sprite sprite, Material material = null)
    {
        if (label == null || flashRenderer == null || sprite == null)
            return;

        flashRenderer.sprite = sprite;
        flashRenderer.color = flashColor;
        if (material != null)
            flashRenderer.sharedMaterial = material;
        flashRenderer.enabled = true;

        FitToHost(sprite);

        label.gameObject.SetActive(false);
        flashTransform.localPosition = Vector3.zero;
        flashTransform.localScale = restScale * startScaleMultiplier;
        scaleVelocity = 0f;
        scaleVelocity = Mathf.Abs(restScale.x * (peakScaleMultiplier - startScaleMultiplier) / Mathf.Max(0.01f, settleSmoothTime));
        holdTimer = 0f;
        settling = true;
        active = true;
    }

    void FitToHost(Sprite sprite)
    {
        float targetWorldSize;
        if (hostBody != null && hostBody.sprite != null)
        {
            float bodySize = Mathf.Min(hostBody.bounds.size.x, hostBody.bounds.size.y);
            targetWorldSize = bodySize * fitFraction;
        }
        else
        {
            targetWorldSize = 0.45f;
        }

        float spriteSize = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
        float worldScale = targetWorldSize / Mathf.Max(spriteSize, 0.001f);
        float parentLossy = flashTransform.parent != null
            ? Mathf.Max(Mathf.Abs(flashTransform.parent.lossyScale.x), 0.001f)
            : 1f;
        float local = worldScale / parentLossy;
        restScale = new Vector3(local, local, 1f);
    }

    public void Update()
    {
        if (!active || flashTransform == null)
            return;

        // Keep the flash upright even if the parent spins.
        flashTransform.rotation = Quaternion.identity;

        if (settling)
        {
            Vector3 peak = restScale * peakScaleMultiplier;
            float next = Mathf.SmoothDamp(
                flashTransform.localScale.x,
                peak.x,
                ref scaleVelocity,
                settleSmoothTime,
                Mathf.Infinity,
                Time.deltaTime);
            flashTransform.localScale = new Vector3(next, next, restScale.z);

            if (Mathf.Abs(next - peak.x) < 0.01f * restScale.x && Mathf.Abs(scaleVelocity) < 0.5f)
            {
                flashTransform.localScale = peak;
                settling = false;
                holdTimer = holdDuration;
            }
            return;
        }

        holdTimer -= Time.deltaTime;
        if (holdTimer > 0f)
            return;

        float shrink = Mathf.SmoothDamp(
            flashTransform.localScale.x,
            restScale.x,
            ref scaleVelocity,
            settleSmoothTime * 0.85f,
            Mathf.Infinity,
            Time.deltaTime);
        flashTransform.localScale = new Vector3(shrink, shrink, restScale.z);

        if (Mathf.Abs(shrink - restScale.x) < 0.02f * restScale.x)
            Finish();
    }

    void Finish()
    {
        active = false;
        settling = false;
        flashRenderer.enabled = false;
        if (label != null)
            label.gameObject.SetActive(true);
    }
}
