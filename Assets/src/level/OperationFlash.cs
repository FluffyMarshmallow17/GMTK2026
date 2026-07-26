using TMPro;
using UnityEngine;

/// <summary>
/// Briefly hides the countdown label and flashes operation/number sprites that pop
/// in, fit inside the host body, stay dark for glow contrast, then restore the
/// number. Can show a single symbol (an operation) or an operation+number combo.
/// </summary>
public class OperationFlash
{
    public float startScaleMultiplier = 0.35f;
    public float peakScaleMultiplier = 1.12f;
    public float settleSmoothTime = 0.1f;
    public float holdDuration = 0.2f;
    [Tooltip("How much of the host body the flash should fill at rest (0-1).")]
    public float fitFraction = 0.55f;
    [Tooltip("Gap between the two symbols in a combo, as a fraction of symbol height.")]
    public float comboSpacing = 0.2f;
    public Color flashColor = new Color(0.05f, 0.05f, 0.07f, 1f);

    TextMeshPro label;
    Transform host;
    SpriteRenderer hostBody;
    Transform flashTransform;                              // container that animates
    readonly SpriteRenderer[] symbols = new SpriteRenderer[2];
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

        var container = new GameObject("OperationFlash");
        flashTransform = container.transform;
        flashTransform.SetParent(host != null ? host : label.transform.parent, false);
        flashTransform.localPosition = Vector3.zero;
        flashTransform.localRotation = Quaternion.identity;

        for (int i = 0; i < symbols.Length; i++)
        {
            var go = new GameObject("Symbol" + i);
            go.transform.SetParent(flashTransform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.color = flashColor;
            sr.sortingOrder = 80;
            sr.enabled = false;
            symbols[i] = sr;
        }
    }

    /// <summary>Flash a single symbol (e.g. an operation on its own).</summary>
    public void Play(Sprite sprite, Material material = null)
    {
        PlaySymbols(sprite, material, null, null);
    }

    /// <summary>Flash an operation and the number applied to it, side by side.</summary>
    public void PlayCombo(Sprite opSprite, Material opMaterial, Sprite numberSprite, Material numberMaterial)
    {
        // Fall back to a single flash if the operation sprite is missing.
        if (opSprite == null)
            PlaySymbols(numberSprite, numberMaterial, null, null);
        else
            PlaySymbols(opSprite, opMaterial, numberSprite, numberMaterial);
    }

    void PlaySymbols(Sprite a, Material aMat, Sprite b, Material bMat)
    {
        if (label == null || flashTransform == null || a == null)
            return;

        Assign(symbols[0], a, aMat);
        Assign(symbols[1], b, bMat);
        LayoutSymbols();

        label.gameObject.SetActive(false);
        flashTransform.localPosition = Vector3.zero;
        flashTransform.localScale = restScale * startScaleMultiplier;
        scaleVelocity = Mathf.Abs(restScale.x * (peakScaleMultiplier - startScaleMultiplier) / Mathf.Max(0.01f, settleSmoothTime));
        holdTimer = 0f;
        settling = true;
        active = true;
    }

    void Assign(SpriteRenderer sr, Sprite sprite, Material material)
    {
        if (sprite == null)
        {
            sr.sprite = null;
            sr.enabled = false;
            return;
        }
        sr.sprite = sprite;
        sr.color = flashColor;
        if (material != null)
            sr.sharedMaterial = material;
        sr.enabled = true;
    }

    // Normalizes each active symbol to unit height, lays them in a centered row, and
    // sets the container rest scale so the whole row fits the host body.
    void LayoutSymbols()
    {
        float[] widths = new float[symbols.Length];
        int count = 0;
        float totalWidth = 0f;

        for (int i = 0; i < symbols.Length; i++)
        {
            if (!symbols[i].enabled || symbols[i].sprite == null) { widths[i] = 0f; continue; }
            Vector2 size = symbols[i].sprite.bounds.size;
            float childScale = 1f / Mathf.Max(size.y, 0.001f); // normalize to 1 unit tall
            symbols[i].transform.localScale = Vector3.one * childScale;
            widths[i] = size.x * childScale;
            totalWidth += widths[i];
            count++;
        }
        if (count == 0)
            return;
        totalWidth += comboSpacing * (count - 1);

        float x = -totalWidth * 0.5f;
        for (int i = 0; i < symbols.Length; i++)
        {
            if (!symbols[i].enabled || symbols[i].sprite == null) continue;
            symbols[i].transform.localPosition = new Vector3(x + widths[i] * 0.5f, 0f, 0f);
            x += widths[i] + comboSpacing;
        }

        float targetWorldSize = (hostBody != null && hostBody.sprite != null)
            ? Mathf.Min(hostBody.bounds.size.x, hostBody.bounds.size.y) * fitFraction
            : 0.45f;

        float assemblyExtent = Mathf.Max(1f, totalWidth); // row is 1 unit tall
        float worldScale = targetWorldSize / assemblyExtent;
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
        for (int i = 0; i < symbols.Length; i++)
            if (symbols[i] != null)
                symbols[i].enabled = false;
        if (label != null)
            label.gameObject.SetActive(true);
    }
}
