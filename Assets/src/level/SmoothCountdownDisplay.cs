using TMPro;
using UnityEngine;

/// <summary>
/// Smooths the visible countdown toward the real value (ease-out via SmoothDamp).
/// Gameplay should keep using the real int; only the label is animated.
/// </summary>
/// 
/// second purely vibecoded file!!
public class SmoothCountdownDisplay
{
    public float smoothTime = 0.35f;
    public float maxSpeed = 0f;

    TextMeshPro label;
    float displayed;
    float velocity;
    int lastShown = int.MinValue;

    public void Init(TextMeshPro display, int value, float smoothTime = 0.35f)
    {
        label = display;
        this.smoothTime = smoothTime;
        Snap(value);
    }

    public void Snap(int value)
    {
        displayed = value;
        velocity = 0f;
        ApplyText(value);
    }

    public void Update(int target)
    {
        if (label == null)
            return;

        float max = maxSpeed > 0f ? maxSpeed : Mathf.Infinity;
        displayed = Mathf.SmoothDamp(displayed, target, ref velocity, smoothTime, max, Time.deltaTime);

        if (float.IsNaN(displayed))
        {
            Snap(target);
            return;
        }

        ApplyText(Mathf.RoundToInt(displayed));
    }

    void ApplyText(int value)
    {
        if (value == lastShown)
            return;
        lastShown = value;
        label.text = "" + value;
    }
}
