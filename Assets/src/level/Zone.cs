using System.Collections.Generic;
using UnityEngine;

// A cost-gated reward zone: a red-tinted circle holding a cluster of same-type
// blocks. Crossing into it charges the player `cost` countdown, then the gate
// (collider + ring + tint) disappears and the blocks are freely accessible.
// If the player never enters, the zone expires after its lifespan and takes its
// blocks with it. Zones are created and configured by LevelManager.
[RequireComponent(typeof(CircleCollider2D))]
public class Zone : MonoBehaviour
{
    public int cost;
    public int size; // radius, typically 5-10
    public operationType type;
    public int number;

    [Header("Look")]
    public int segments = 64;
    public float lineWidth = 0.08f;
    [ColorUsage(true, true)] public Color lineColor = new Color(2f, 0.25f, 0.2f, 1f);
    [Tooltip("Tint hue (matches the outer grid by default). Opacity comes from Fill Opacity.")]
    [ColorUsage(true, true)] public Color fillColor = new Color(2f, 0.25f, 0.2f, 1f);
    [Range(0f, 1f)]
    [Tooltip("How faint the inner tint is — keep it low, like the outer grid.")]
    public float fillOpacity = 0.08f;
    [Tooltip("Time to grow from 0 to full radius when the zone appears (ease-out).")]
    public float growDuration = 0.35f;
    [Tooltip("Time to shrink back to 0 when unlocked (ease-in / accelerating).")]
    public float shrinkDuration = 0.3f;

    [Header("Cost number")]
    [Tooltip("Digit sprites indexed by value: element 0 = \"0\", 1 = \"1\" … 9 = \"9\". Use the number PNGs.")]
    public Sprite[] digitSprites;
    [Tooltip("Tint for the digits (the number PNGs are already white).")]
    public Color costColor = Color.white;
    [Tooltip("Digit height as a fraction of the zone radius (bigger = huger).")]
    public float costHeightFactor = 0.4f;
    [Tooltip("Gap between digits, in digit-widths.")]
    public float costDigitSpacing = 0.1f;
    [Tooltip("Optional glow material for the digits (e.g. selectGlow). Leave empty for plain.")]
    public Material digitMaterial;

    CircleCollider2D circle;
    LineRenderer border;
    SpriteRenderer fill;
    Transform costRoot;
    float costBaseScale = 1f;
    readonly List<GameObject> blocks = new List<GameObject>();
    float lifespan;
    float age;
    bool consumed;

    float targetRadius;
    float currentRadius;
    float growElapsed;
    bool growing;

    bool shrinking;
    float shrinkElapsed;
    float shrinkFromRadius;
    bool takeBlocksOnShrink;

    // Called by LevelManager right after Instantiate. (Not Awake: the manager
    // sets our size/cost/type after instantiation, which Awake would miss.)
    public void Initialize(int size, int cost, float lifespan, bool isNumber, int typeIndex, int blockCount, GameObject blockPrefab)
    {
        this.size = size;
        this.cost = cost;
        this.lifespan = lifespan;

        if (isNumber) { number = typeIndex + 1; type = operationType.Empty; }
        else { type = (operationType)typeIndex; number = 0; }

        circle = GetComponent<CircleCollider2D>();
        circle.isTrigger = true;

        targetRadius = size;
        currentRadius = 0f;
        growElapsed = 0f;
        growing = growDuration > 0f;

        BuildBorder();
        BuildFill();
        BuildCostNumber();
        ApplyRadius(growing ? 0f : targetRadius);
        SpawnBlocks(isNumber, typeIndex, blockCount, blockPrefab);
    }

    // Builds the cost as a row of digit sprites (the number PNGs), centered on the zone.
    void BuildCostNumber()
    {
        var container = new GameObject("ZoneCost");
        container.transform.SetParent(transform, false);
        costRoot = container.transform;

        string digits = Mathf.Max(0, cost).ToString();
        var placed = new List<Transform>();
        float x = 0f;
        float rightEdge = 0f;
        float maxHeight = 0.0001f;

        foreach (char ch in digits)
        {
            int value = ch - '0';
            Sprite sprite = (digitSprites != null && value >= 0 && value < digitSprites.Length) ? digitSprites[value] : null;
            if (sprite == null) continue;

            var go = new GameObject("Digit" + value);
            go.transform.SetParent(container.transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = costColor;
            sr.sortingOrder = 6; // above the fill, ring, and blocks
            if (digitMaterial != null)
                sr.sharedMaterial = digitMaterial;

            Vector2 boundsSize = sprite.bounds.size;
            Vector2 boundsCenter = sprite.bounds.center;
            float width = boundsSize.x;
            // Place so the digit's visual center sits at x + width/2 (pivot-agnostic).
            go.transform.localPosition = new Vector3(x + width * 0.5f - boundsCenter.x, -boundsCenter.y, 0f);

            rightEdge = x + width;
            x = rightEdge + width * costDigitSpacing;
            maxHeight = Mathf.Max(maxHeight, boundsSize.y);
            placed.Add(go.transform);
        }

        // Center the whole row horizontally around the zone center.
        float shift = rightEdge * 0.5f;
        foreach (Transform t in placed)
            t.localPosition -= new Vector3(shift, 0f, 0f);

        // Scale so a digit is `costHeightFactor` of the radius tall.
        costBaseScale = (size * costHeightFactor) / maxHeight;
    }

    // A ring showing the zone's size (same idea as Map.cs's border).
    void BuildBorder()
    {
        border = GetComponent<LineRenderer>();
        if (border == null) border = gameObject.AddComponent<LineRenderer>();
        border.loop = true;
        border.useWorldSpace = true;
        border.widthMultiplier = lineWidth;
        border.positionCount = segments;
        border.numCornerVertices = 2;
        border.numCapVertices = 2;
        border.sortingOrder = 5;
        border.startColor = border.endColor = lineColor;
        if (border.sharedMaterial == null)
            border.material = new Material(Shader.Find("Sprites/Default"));
    }

    // Red tint filling the circle, drawn behind the blocks. Opacity is fixed at half.
    void BuildFill()
    {
        var go = new GameObject("ZoneFill");
        go.transform.SetParent(transform, false);
        fill = go.AddComponent<SpriteRenderer>();
        fill.sprite = CircleSprite();
        fill.color = new Color(fillColor.r, fillColor.g, fillColor.b, fillOpacity);
        fill.sortingOrder = -1;
    }

    // Drives the collider, ring, and tint to a given radius (used by the grow-in).
    void ApplyRadius(float radius)
    {
        currentRadius = radius;

        if (circle != null)
            circle.radius = Mathf.Max(0.01f, radius);

        if (border != null)
        {
            Vector3 c = transform.position;
            for (int i = 0; i < segments; i++)
            {
                float a = i / (float)segments * Mathf.PI * 2f;
                border.SetPosition(i, c + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * radius);
            }
        }

        if (fill != null)
            fill.transform.localScale = Vector3.one * (radius * 2f); // circle sprite is 1 world unit wide

        // Grow/shrink the cost number in step with the zone (0 at radius 0, full at target).
        if (costRoot != null)
        {
            float progress = targetRadius > 0f ? Mathf.Clamp01(radius / targetRadius) : 1f;
            costRoot.localScale = Vector3.one * (costBaseScale * progress);
        }
    }

    // Spawn `blockCount` blocks, all of the zone's rolled type, scattered inside the circle.
    void SpawnBlocks(bool isNumber, int typeIndex, int blockCount, GameObject blockPrefab)
    {
        if (blockPrefab == null) return;

        for (int i = 0; i < blockCount; i++)
        {
            float r = size * Mathf.Sqrt(Random.value); // sqrt = uniform over the disc
            float a = Random.value * Mathf.PI * 2f;
            Vector3 pos = transform.position + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * r;

            GameObject blockObj = Instantiate(blockPrefab, pos, Quaternion.identity);
            Block block = blockObj.GetComponent<Block>();
            if (block != null)
            {
                if (isNumber) block.SetNumber(typeIndex);
                else block.SetOperation((operationType)typeIndex);
            }
            blocks.Add(blockObj);
        }
    }

    void Update()
    {
        // Unlock animation takes priority and runs even after "consumed".
        if (shrinking)
        {
            shrinkElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(shrinkElapsed / shrinkDuration);
            float eased = t * t * t; // ease-in: accelerates as it collapses
            ApplyRadius(Mathf.Lerp(shrinkFromRadius, 0f, eased));
            if (t >= 1f)
                Finish();
            return;
        }

        if (consumed) return;

        // Grow in from 0 to full radius, fast then slowing (ease-out cubic).
        if (growing)
        {
            growElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(growElapsed / growDuration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            ApplyRadius(Mathf.Lerp(0f, targetRadius, eased));
            if (t >= 1f)
            {
                growing = false;
                ApplyRadius(targetRadius);
            }
        }

        if (lifespan > 0f)
        {
            age += Time.deltaTime;
            if (age >= lifespan) Expire();
        }
    }

    public void OnTriggerEnter2D(Collider2D collider)
    {
        if (consumed || !collider.transform.CompareTag("Player")) return;

        Player player = collider.transform.GetComponent<Player>();
        if (player != null)
            player.setCountdown(player.getCountdown() - cost);

        // Zone entered: collapse the gate/visuals, leave the blocks for the player.
        consumed = true;
        BeginShrink();
    }

    // Disable the trigger and accelerate the radius down to 0, then remove the zone.
    void BeginShrink()
    {
        if (circle != null) circle.enabled = false;
        shrinkFromRadius = currentRadius;
        shrinkElapsed = 0f;
        shrinking = shrinkDuration > 0f;
        if (!shrinking)
            Finish();
    }

    // Removes the zone once the shrink completes, taking its blocks if it expired.
    void Finish()
    {
        if (takeBlocksOnShrink)
            foreach (GameObject b in blocks)
                if (b != null) Destroy(b);
        Destroy(gameObject);
    }

    // Timed out before the player paid the cost: shrink away, taking the reward with it.
    void Expire()
    {
        if (consumed) return;
        consumed = true;
        takeBlocksOnShrink = true;
        BeginShrink();
    }

    // A soft-edged white circle sprite, generated once and reused (tinted per zone).
    static Sprite cachedCircle;
    static Sprite CircleSprite()
    {
        if (cachedCircle != null) return cachedCircle;

        const int res = 128;
        var tex = new Texture2D(res, res, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        float c = (res - 1) / 2f;
        for (int y = 0; y < res; y++)
            for (int x = 0; x < res; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c)) / c;
                float alpha = d <= 1f ? 1f : 0f; // hard edge — the border line covers it
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        tex.Apply();
        cachedCircle = Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), res);
        return cachedCircle;
    }
}
