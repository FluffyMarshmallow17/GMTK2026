using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Ambient menu / win-lose backdrop: random grid pulses and drifting number blocks.
/// </summary>
public class AmbientGridPulses : MonoBehaviour
{
    [Header("Grid Pulses")]
    public Vector2 intervalRange = new Vector2(1.2f, 3.5f);
    public Vector2 strengthRange = new Vector2(0.35f, 0.9f);
    [Range(0f, 0.5f)]
    public float viewPadding = 0.1f;

    [Header("Floating Blocks")]
    public bool spawnBlocks = true;
    [Tooltip("Drag Assets/prefabs/Block.prefab here.")]
    public GameObject blockPrefab;
    public int maxBlockCount = 8;
    public Vector2 blockSpawnInterval = new Vector2(0.35f, 1.2f);
    public Vector2 blockLifetime = new Vector2(4f, 9f);
    public Vector2 blockInitialSpeed = new Vector2(0.15f, 0.75f);
    public Vector2 blockForce = new Vector2(0.12f, 0.45f);
    public float blockDrag = 0.28f;
    [Range(0f, 1f)]
    public float blockEdgeBias = 0.55f;
    [Range(0.05f, 0.45f)]
    public float blockBorderThickness = 0.2f;
    [Range(0.1f, 1f)]
    public float blockAlpha = 0.55f;
    public float blockScale = 0.55f;

    [Header("Timing")]
    public bool useUnscaledTime = true;

    struct BlockFloater
    {
        public Block block;
        public SpriteRenderer sprite;
        public Vector2 velocity;
        public float age;
        public float lifetime;
        public float nextForceAt;
        public bool active;
    }

    float nextPulseAt;
    int pulseSourceCounter;

    readonly List<BlockFloater> floaters = new List<BlockFloater>();
    Transform blockRoot;
    float nextBlockSpawnAt;
    bool blocksReady;
    int numberSpriteCount;
    bool loggedMissingPrefab;

    float DeltaTime => useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
    float Now => useUnscaledTime ? Time.unscaledTime : Time.time;

    void OnEnable()
    {
        ScheduleNextPulse();
        nextBlockSpawnAt = 0f;
        EnsureBlocks();
        PrimeBlocks();
    }

    void OnDisable()
    {
        if (blockRoot != null)
            Destroy(blockRoot.gameObject);
        blockRoot = null;
        floaters.Clear();
        blocksReady = false;
    }

    void Update()
    {
        UpdatePulses();
        UpdateBlocks();
    }

    void UpdatePulses()
    {
        float now = Now;
        if (now < nextPulseAt)
            return;

        FirePulse();
        ScheduleNextPulse();
    }

    void FirePulse()
    {
        Camera cam = Camera.main;
        if (cam == null || !cam.orthographic)
            return;

        GetViewHalfExtents(cam, out float halfW, out float halfH, out Vector2 center);
        Vector2 pos = new Vector2(
            center.x + Random.Range(-halfW, halfW),
            center.y + Random.Range(-halfH, halfH));
        float strength = Random.Range(strengthRange.x, strengthRange.y);

        pulseSourceCounter++;
        GridBackground.Pulse(pos, strength, default, GetInstanceID() ^ pulseSourceCounter);
    }

    void ScheduleNextPulse()
    {
        float now = Now;
        float min = Mathf.Min(intervalRange.x, intervalRange.y);
        float max = Mathf.Max(intervalRange.x, intervalRange.y);
        nextPulseAt = now + Random.Range(Mathf.Max(0.05f, min), Mathf.Max(0.05f, max));
    }

    void EnsureBlocks()
    {
        if (!spawnBlocks || blocksReady)
            return;

        GameObject prefab = ResolveBlockPrefab();
        if (prefab == null)
        {
            if (!loggedMissingPrefab)
            {
                Debug.LogWarning("AmbientGridPulses: assign Block Prefab on AmbientGridPulses (Assets/prefabs/Block.prefab).");
                loggedMissingPrefab = true;
            }
            return;
        }

        Block sample = prefab.GetComponent<Block>();
        if (sample == null)
        {
            Debug.LogWarning("AmbientGridPulses: blockPrefab has no Block component.");
            return;
        }

        numberSpriteCount = sample.NumberSpriteCount;
        blockRoot = new GameObject("AmbientBlocks").transform;
        blockRoot.SetParent(transform, false);

        int count = Mathf.Max(1, maxBlockCount);
        for (int i = 0; i < count; i++)
            floaters.Add(CreateBlockFloater(prefab));

        blocksReady = true;
    }

    GameObject ResolveBlockPrefab()
    {
        if (blockPrefab != null)
            return blockPrefab;

#if UNITY_EDITOR
        blockPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/prefabs/Block.prefab");
        if (blockPrefab != null)
            return blockPrefab;
#endif
        return null;
    }

    BlockFloater CreateBlockFloater(GameObject prefab)
    {
        Block block = Instantiate(prefab, blockRoot).GetComponent<Block>();
        PrepareDecorativeBlock(block);

        Transform spriteTransform = block.transform.Find("Sprite");
        SpriteRenderer sprite = spriteTransform != null
            ? spriteTransform.GetComponent<SpriteRenderer>()
            : block.GetComponentInChildren<SpriteRenderer>();

        if (sprite != null)
            sprite.sortingOrder = 5;

        block.gameObject.SetActive(false);
        return new BlockFloater { block = block, sprite = sprite };
    }

    static void PrepareDecorativeBlock(Block block)
    {
        Rigidbody2D rb = block.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.simulated = false;

        foreach (Collider2D collider in block.GetComponentsInChildren<Collider2D>())
            collider.enabled = false;
    }

    void PrimeBlocks()
    {
        if (!spawnBlocks || !blocksReady)
            return;

        Camera cam = Camera.main;
        if (cam == null || !cam.orthographic)
            return;

        int initial = Mathf.Min(maxBlockCount, Mathf.Max(2, maxBlockCount / 2));
        for (int i = 0; i < initial; i++)
        {
            if (!SpawnBlock(cam))
                break;
        }
        ScheduleNextBlockSpawn(Now);
    }

    void UpdateBlocks()
    {
        if (!spawnBlocks)
            return;

        EnsureBlocks();
        if (!blocksReady)
            return;

        Camera cam = Camera.main;
        if (cam == null || !cam.orthographic)
            return;

        float now = Now;
        float dt = DeltaTime;
        int activeCount = 0;

        for (int i = 0; i < floaters.Count; i++)
        {
            BlockFloater f = floaters[i];
            if (!f.active)
                continue;

            activeCount++;
            f.age += dt;

            if (f.age >= f.lifetime)
            {
                f.active = false;
                f.block.gameObject.SetActive(false);
                floaters[i] = f;
                continue;
            }

            if (now >= f.nextForceAt)
            {
                f.velocity += Random.insideUnitCircle * Random.Range(blockForce.x, blockForce.y);
                f.nextForceAt = now + Random.Range(0.35f, 1.1f);
            }

            f.velocity *= Mathf.Exp(-blockDrag * dt);
            Vector3 pos = f.block.transform.position;
            pos += (Vector3)(f.velocity * dt);
            pos.z = 0f;
            f.block.transform.position = pos;

            if (f.sprite != null)
            {
                float alpha = blockAlpha;
                alpha *= Mathf.Clamp01(f.age / 0.35f);
                alpha *= Mathf.Clamp01((f.lifetime - f.age) / 1.1f);
                Color c = f.sprite.color;
                c.a = alpha;
                f.sprite.color = c;
            }

            floaters[i] = f;
        }

        if (activeCount < maxBlockCount && now >= nextBlockSpawnAt)
        {
            if (SpawnBlock(cam))
                ScheduleNextBlockSpawn(now);
        }
    }

    bool SpawnBlock(Camera cam)
    {
        if (numberSpriteCount <= 0)
            return false;

        for (int i = 0; i < floaters.Count; i++)
        {
            BlockFloater f = floaters[i];
            if (f.active)
                continue;

            Vector2 pos = RandomBlockPoint(cam);
            f.block.transform.position = new Vector3(pos.x, pos.y, 0f);
            f.block.transform.localScale = Vector3.one * blockScale;
            f.block.SetNumber(Random.Range(0, numberSpriteCount));

            Vector2 inward = ((Vector2)cam.transform.position - pos).normalized;
            if (inward.sqrMagnitude < 0.001f)
                inward = Random.insideUnitCircle.normalized;

            Vector2 tangent = new Vector2(-inward.y, inward.x);
            float speed = Random.Range(blockInitialSpeed.x, blockInitialSpeed.y);
            f.velocity = (inward * Random.Range(0.35f, 0.85f) + tangent * Random.Range(-0.65f, 0.65f)).normalized * speed;
            f.age = 0f;
            f.lifetime = Random.Range(blockLifetime.x, blockLifetime.y);
            f.nextForceAt = Now + Random.Range(0.2f, 0.8f);
            f.active = true;

            if (f.sprite != null)
            {
                Color c = f.sprite.color;
                c.a = 0f;
                f.sprite.color = c;
            }

            f.block.gameObject.SetActive(true);
            floaters[i] = f;
            return true;
        }

        return false;
    }

    Vector2 RandomBlockPoint(Camera cam)
    {
        GetViewHalfExtents(cam, out float halfW, out float halfH, out Vector2 center);

        Vector2 anywhere = center + new Vector2(Random.Range(-halfW, halfW), Random.Range(-halfH, halfH));

        float angle = Random.Range(0f, Mathf.PI * 2f);
        float cos = Mathf.Cos(angle);
        float sin = Mathf.Sin(angle);
        float edgeT = Mathf.Min(
            Mathf.Abs(cos) > 0.001f ? halfW / Mathf.Abs(cos) : float.MaxValue,
            Mathf.Abs(sin) > 0.001f ? halfH / Mathf.Abs(sin) : float.MaxValue);
        float inner = Mathf.Clamp01(1f - blockBorderThickness);
        Vector2 border = center + new Vector2(cos, sin) * edgeT * Random.Range(inner, 1f);

        return Vector2.Lerp(anywhere, border, blockEdgeBias);
    }

    void GetViewHalfExtents(Camera cam, out float halfW, out float halfH, out Vector2 center)
    {
        halfH = cam.orthographicSize * (1f + viewPadding);
        halfW = halfH * cam.aspect;
        center = cam.transform.position;
    }

    void ScheduleNextBlockSpawn(float now)
    {
        float min = Mathf.Min(blockSpawnInterval.x, blockSpawnInterval.y);
        float max = Mathf.Max(blockSpawnInterval.x, blockSpawnInterval.y);
        nextBlockSpawnAt = now + Random.Range(Mathf.Max(0.05f, min), Mathf.Max(0.05f, max));
    }

    public static void ActivateUnder(Transform root)
    {
        if (root == null)
            return;

        AmbientGridPulses pulses = root.GetComponentInChildren<AmbientGridPulses>(true);
        if (pulses == null)
            return;

        pulses.gameObject.SetActive(true);
        GridBackground grid = pulses.GetComponent<GridBackground>();
        if (grid != null)
            grid.ClaimAsActiveInstance();
    }
}
