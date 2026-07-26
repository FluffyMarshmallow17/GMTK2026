using UnityEngine;

// A single floating, glowing level number in the world-space level select.
// The SceneLoader spawns these and drives selection; this component just
// handles the idle bob, glow pulse, and the "selected" emphasis.
[RequireComponent(typeof(SpriteRenderer))]
public class LevelIcon : MonoBehaviour
{
    public int levelNumber;
    public bool selectable = true;

    [Header("Float")]
    [Tooltip("Vertical bob height (world units).")]
    public float bobAmplitude = 0.15f;
    [Tooltip("Horizontal drift width (world units).")]
    public float driftAmplitude = 0.06f;
    public float bobSpeed = 1.5f;

    [Header("Selected emphasis")]
    [Tooltip("Scale multiplier applied while this level is highlighted.")]
    public float selectedScale = 1.25f;
    public float scaleSmoothTime = 0.12f;

    [Header("Glow pulse")]
    [Tooltip("Brightness swing of the glow (0 = none).")]
    public float glowPulse = 0.15f;
    public float glowSpeed = 2f;
    [Tooltip("Color/opacity multiplier applied to locked levels (slightly faded).")]
    public Color lockedTint = new Color(0.55f, 0.55f, 0.6f, 0.2f);

    SpriteRenderer sr;
    Vector3 home;
    Vector3 spawnScale;
    float phase;
    bool selected;
    float scaleVel;
    Color baseColor;

    // Called by SceneLoader right after the icon is positioned and scaled.
    public void Init(int number, bool isSelectable)
    {
        sr = GetComponent<SpriteRenderer>();
        levelNumber = number;
        selectable = isSelectable;
        home = transform.localPosition;
        spawnScale = transform.localScale;
        phase = Random.value * Mathf.PI * 2f; // desync each icon's bob/glow
        baseColor = sr.color;
        if (!selectable)
            sr.color = baseColor * lockedTint;
    }

    public void SetSelected(bool value)
    {
        selected = value;
    }

    void Update()
    {
        float t = Time.time * bobSpeed + phase;

        // Idle float in place.
        Vector3 offset = new Vector3(
            Mathf.Cos(t * 0.5f) * driftAmplitude,
            Mathf.Sin(t) * bobAmplitude,
            0f);
        transform.localPosition = home + offset;

        // Ease toward the selected/normal scale.
        float targetScale = spawnScale.x * (selected ? selectedScale : 1f);
        float next = Mathf.SmoothDamp(transform.localScale.x, targetScale, ref scaleVel, scaleSmoothTime);
        transform.localScale = new Vector3(next, next, spawnScale.z);

        // Glowing pulse (only for playable levels).
        if (selectable)
        {
            float pulse = glowPulse * (selected ? 1.6f : 1f);
            float g = 1f + Mathf.Sin(Time.time * glowSpeed + phase) * pulse;
            sr.color = new Color(baseColor.r * g, baseColor.g * g, baseColor.b * g, baseColor.a);
        }
    }
}
