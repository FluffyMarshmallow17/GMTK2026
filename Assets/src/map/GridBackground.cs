using UnityEngine;

/// <summary>
/// Fullscreen responsive grid behind the map. Blue inside the map circle, glowing red
/// outside, and emits glow pulses along the grid lines when countdowns decrease.
/// Lives on the Map object; creates its own quad + material instance at runtime.
/// </summary>
public class GridBackground : MonoBehaviour
{
    const int MaxPulses = 16;
    const int MaxSpots = 64;

    [Tooltip("How far a pulse ring travels over its lifetime, world units.")]
    public float pulseDistance = 10f;
    [Tooltip("How long a pulse lives before fully fading, seconds.")]
    public float pulseDuration = 2f;
    [Tooltip("Size of the background quad in world units.")]
    public float backgroundSize = 300f;
    [Tooltip("How long a newly spawned block takes to fade its grid glow in, seconds.")]
    public float spotFadeInDuration = 0.6f;
    [Tooltip("Global cap on how many pulses can spawn per second; extra requests are dropped.")]
    public float maxPulsesPerSecond = 2f;
    public int sortingOrder = -100;

    static GridBackground instance;

    Material material;
    CircleCollider2D mapCollider;
    readonly Vector4[] pulses = new Vector4[MaxPulses];
    readonly Vector2[] pulsePositions = new Vector2[MaxPulses];
    readonly float[] pulseAges = new float[MaxPulses];
    readonly bool[] pulseActive = new bool[MaxPulses];
    readonly float[] pulseSeeds = new float[MaxPulses];
    readonly float[] pulseStrengths = new float[MaxPulses];
    readonly Vector2[] pulseVelocities = new Vector2[MaxPulses];

    // Objects (blocks) that make the grid underneath them glow softly.
    static readonly System.Collections.Generic.List<Transform> spotSources =
        new System.Collections.Generic.List<Transform>();
    static readonly System.Collections.Generic.List<float> spotAges =
        new System.Collections.Generic.List<float>();
    static readonly System.Collections.Generic.List<float> spotSeeds =
        new System.Collections.Generic.List<float>();
    readonly Vector4[] spots = new Vector4[MaxSpots];
    readonly Vector4[] spotAnim = new Vector4[MaxSpots];

    void Awake()
    {
        instance = this;
        mapCollider = GetComponent<CircleCollider2D>();

        Material sourceMaterial = Resources.Load<Material>("GridBackground");
        if (sourceMaterial == null)
        {
            Debug.LogWarning("GridBackground material not found in Resources; grid disabled.");
            enabled = false;
            return;
        }
        // Instance the material so runtime uniform updates don't dirty the asset.
        material = new Material(sourceMaterial);

        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "GridBackgroundQuad";
        Destroy(quad.GetComponent<Collider>());
        quad.transform.SetParent(transform, false);
        quad.transform.localPosition = new Vector3(0f, 0f, 10f);
        quad.transform.localScale = new Vector3(backgroundSize, backgroundSize, 1f);

        MeshRenderer quadRenderer = quad.GetComponent<MeshRenderer>();
        quadRenderer.sharedMaterial = material;
        quadRenderer.sortingOrder = sortingOrder;
        quadRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        quadRenderer.receiveShadows = false;
        quadRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
    }

    void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    void Update()
    {
        if (material == null)
            return;

        material.SetVector("_MapCenter", transform.position);
        if (mapCollider != null)
            material.SetFloat("_MapRadius", mapCollider.radius * Mathf.Abs(transform.lossyScale.x));

        for (int i = 0; i < MaxPulses; i++)
        {
            if (!pulseActive[i])
            {
                pulses[i] = Vector4.zero;
                continue;
            }

            pulseAges[i] += Time.deltaTime;
            float t = pulseAges[i] / pulseDuration;
            if (t >= 1f)
            {
                pulseActive[i] = false;
                pulses[i] = Vector4.zero;
                continue;
            }

            // The ring's center drifts with the emitter's velocity (decaying over
            // the pulse's life) so ripples don't visibly lag behind a moving player.
            pulsePositions[i] += pulseVelocities[i] * ((1f - t) * Time.deltaTime);

            // Water-ripple motion: the ring surges outward then decelerates
            // (quadratic ease-out) while its energy dissipates over the lifetime.
            float travel = 1f - (1f - t) * (1f - t);
            float radius = travel * pulseDistance;
            float strength = (1f - t) * (1f - t) * pulseStrengths[i];
            pulses[i] = new Vector4(pulsePositions[i].x, pulsePositions[i].y, radius, strength);
        }

        material.SetVectorArray("_Pulses", pulses);
        material.SetFloatArray("_PulseSeeds", pulseSeeds);

        // Standing glow spots under registered blocks, easing in after spawn so
        // the glow never pops into existence. Each spot is animated: the core
        // breathes, and blobs of glow wander outward along the grid lines.
        int spotCount = 0;
        float now = Time.time;
        for (int i = spotSources.Count - 1; i >= 0; i--)
        {
            if (spotSources[i] == null)
            {
                spotSources.RemoveAt(i);
                spotAges.RemoveAt(i);
                spotSeeds.RemoveAt(i);
                continue;
            }
            spotAges[i] += Time.deltaTime;
            if (spotCount >= MaxSpots)
                continue;

            float t = Mathf.Clamp01(spotAges[i] / Mathf.Max(spotFadeInDuration, 0.01f));
            float fade = t * t * (3f - 2f * t);
            float seed = spotSeeds[i];

            // Pronounced breathing of the core glow: swings from a faint ember
            // to well above its base brightness.
            float flicker = 0.25f + 1.5f * Mathf.PerlinNoise(seed, now * 1.4f);
            // Glow blobs sliding along the horizontal/vertical lines through the
            // block (offsets in units of the spot radius, resolved in the shader).
            float offX = (Mathf.PerlinNoise(seed + 11.3f, now * 0.6f) - 0.5f) * 7f;
            float offY = (Mathf.PerlinNoise(seed + 27.9f, now * 0.7f) - 0.5f) * 7f;
            // Arms come and go: dormant stretches, then hard flare-ups brighter
            // than the core itself.
            float armStrength = Mathf.Clamp01(Mathf.PerlinNoise(seed + 5.1f, now * 0.9f) * 2f - 0.6f) * 2f;

            Vector3 pos = spotSources[i].position;
            spots[spotCount] = new Vector4(pos.x, pos.y, flicker, fade);
            spotAnim[spotCount] = new Vector4(offX, offY, armStrength, 0f);
            spotCount++;
        }
        material.SetInt("_SpotCount", spotCount);
        material.SetVectorArray("_Spots", spots);
        material.SetVectorArray("_SpotAnim", spotAnim);
    }

    /// <summary>Make the grid underneath this transform glow softly while it exists.</summary>
    public static void RegisterSpot(Transform source)
    {
        if (spotSources.Contains(source))
            return;
        spotSources.Add(source);
        spotAges.Add(0f);
        spotSeeds.Add(Random.Range(0f, 100f));
    }

    public static void UnregisterSpot(Transform source)
    {
        int index = spotSources.IndexOf(source);
        if (index < 0)
            return;
        spotSources.RemoveAt(index);
        spotAges.RemoveAt(index);
        spotSeeds.RemoveAt(index);
    }

    /// <summary>
    /// Emit a glow ring from a world position (call when a countdown decreases).
    /// Pass the emitter's velocity so the ring leads and follows a moving emitter.
    /// </summary>
    static readonly System.Collections.Generic.Dictionary<int, float> lastPulseBySource =
        new System.Collections.Generic.Dictionary<int, float>();

    public static void Pulse(Vector2 worldPosition, float strength = 1f, Vector2 velocity = default, int sourceId = 0)
    {
        if (instance == null)
            return;

        // Rate-limit per emitter so rapid decay on the player doesn't steal
        // pulses from the boss / minis ticking on the same frame.
        float minInterval = 1f / Mathf.Max(instance.maxPulsesPerSecond, 0.01f);
        if (lastPulseBySource.TryGetValue(sourceId, out float lastTime)
            && Time.time - lastTime < minInterval)
            return;
        lastPulseBySource[sourceId] = Time.time;

        // Prefer a free slot so an in-flight pulse is never cut short; if every
        // slot is busy, recycle the oldest (most faded) one.
        int slot = -1;
        float oldestAge = -1f;
        for (int i = 0; i < MaxPulses; i++)
        {
            if (!instance.pulseActive[i])
            {
                slot = i;
                break;
            }
            if (instance.pulseAges[i] > oldestAge)
            {
                oldestAge = instance.pulseAges[i];
                slot = i;
            }
        }

        instance.pulseActive[slot] = true;
        instance.pulseAges[slot] = 0f;
        // Small forward lead so the ring is born where the emitter is headed.
        instance.pulsePositions[slot] = worldPosition + velocity * 0.05f;
        instance.pulseVelocities[slot] = velocity;
        instance.pulseSeeds[slot] = Random.Range(0f, 100f);
        // Each pulse rolls its own overall glow level so no two feel identical.
        instance.pulseStrengths[slot] = strength * Random.Range(0.5f, 1f);
    }

    /// <summary>
    /// Emit a pulse whose strength scales with how drastically a countdown changed.
    /// A small tick (e.g. 100 -> 99) gives a normal pulse; halving or doubling gives
    /// roughly a 2.5x pulse, and bigger ratio jumps scale up from there (capped at 4x).
    /// </summary>
    public static void PulseFromChange(Vector2 worldPosition, float oldValue, float newValue, Vector2 velocity = default, int sourceId = 0, float strengthScale = 1f)
    {
        if (oldValue <= 0f || newValue <= 0f)
        {
            Pulse(worldPosition, 2f * strengthScale, velocity, sourceId);
            return;
        }

        // |log2(new/old)|: 0 for no change, 1 for a doubling or halving.
        float magnitude = Mathf.Abs(Mathf.Log(newValue / oldValue, 2f));
        float strength = Mathf.Clamp(1f + magnitude * 1.5f, 1f, 4f) * strengthScale;
        Pulse(worldPosition, strength, velocity, sourceId);
    }
}
