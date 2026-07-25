using UnityEngine;

/// <summary>
/// Small trauma-based camera shake. Call CameraShake.ShakeFromChange(oldValue, newValue)
/// when a countdown is changed by an operation; shake strength grows with how drastic
/// the change was, but on a heavily damped (logarithmic) scale.
/// Attaches itself to the main camera on first use.
/// </summary>
public class CameraShake : MonoBehaviour
{
    [Tooltip("How quickly the shake dies down, trauma per second.")]
    public float decay = 1.6f;
    [Tooltip("Camera offset at full trauma, world units.")]
    public float maxOffset = 0.85f;
    [Tooltip("How fast the camera jitters.")]
    public float frequency = 16f;

    static CameraShake instance;

    float trauma;
    Vector3 lastOffset;
    float seed;

    void Awake()
    {
        seed = Random.Range(0f, 1000f);
    }

    void LateUpdate()
    {
        // Undo last frame's offset first so the follow camera's own smoothing
        // never accumulates our jitter.
        transform.position -= lastOffset;
        lastOffset = Vector3.zero;

        if (trauma <= 0f)
            return;

        trauma = Mathf.Max(0f, trauma - decay * Time.deltaTime);

        // Squaring trauma makes small shakes feel subtle and big ones punchy,
        // while Perlin noise keeps the motion smooth instead of jittery-random.
        float amount = trauma * trauma * maxOffset;
        float t = Time.time * frequency;
        lastOffset = new Vector3(
            (Mathf.PerlinNoise(seed, t) - 0.5f) * 2f * amount,
            (Mathf.PerlinNoise(seed + 47.3f, t) - 0.5f) * 2f * amount,
            0f);
        transform.position += lastOffset;
    }

    /// <summary>Shake proportionally to a countdown change, heavily damped.</summary>
    public static void ShakeFromChange(float oldValue, float newValue)
    {
        // |log2(new/old)| so a doubling/halving counts as 1; damped further below.
        float magnitude;
        if (oldValue <= 0f || newValue <= 0f)
            magnitude = 1.5f;
        else
            magnitude = Mathf.Abs(Mathf.Log(newValue / oldValue, 2f));

        // Base kick plus a small ramp, capped: tiny and huge changes stay in
        // the same ballpark rather than differing wildly.
        AddTrauma(0.55f + 0.18f * Mathf.Min(magnitude, 3f));
    }

    static void AddTrauma(float amount)
    {
        if (instance == null)
        {
            // Camera.main needs the "MainCamera" tag; fall back to any camera.
            Camera cam = Camera.main;
            if (cam == null)
                cam = FindAnyObjectByType<Camera>();
            if (cam == null)
                return;
            instance = cam.GetComponent<CameraShake>();
            if (instance == null)
                instance = cam.gameObject.AddComponent<CameraShake>();
        }

        instance.trauma = Mathf.Clamp01(instance.trauma + amount);
    }
}
