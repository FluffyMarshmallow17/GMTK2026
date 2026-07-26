using UnityEngine;

public class Map : MonoBehaviour
{
    public float mapSize = 1f;
    public int segments = 64;
    [Tooltip("Fallback width if the grid material can't be read. Normally the border matches the grid's line thickness automatically.")]
    public float lineWidth = 0.024f;
    [ColorUsage(true, true)]
    public Color lineColor = new Color(2f, 0.25f, 0.2f, 1f);
    [Tooltip("Time to roughly reach the target radius. Lower = snappier.")]
    public float radiusSmoothTime = 0.45f;
    [Tooltip("Optional cap on how fast the radius can change. 0 = uncapped.")]
    public float radiusMaxSpeed = 0f;

    CircleCollider2D circleCollider;
    LineRenderer border;
    float currentRadius;
    float destinationRadius;
    float radiusVelocity;

    void Awake()
    {
        circleCollider = GetComponent<CircleCollider2D>();

        // Keep transform scale fixed so line thickness stays constant.
        transform.localScale = Vector3.one;

        if (GetComponent<GridBackground>() == null)
            gameObject.AddComponent<GridBackground>();
        
        border = GetComponent<LineRenderer>();

        border.loop = true;
        border.useWorldSpace = true;
        border.widthMultiplier = GridLineThickness();
        border.positionCount = segments;
        border.numCornerVertices = 2;
        border.numCapVertices = 2;
        border.sortingOrder = 10;
        border.startColor = lineColor;
        border.endColor = lineColor;

        currentRadius = Mathf.Max(0.01f, circleCollider.radius);
        destinationRadius = currentRadius;
        radiusVelocity = 0f;
        ApplyRadius(currentRadius);
    }

    void Update()
    {
        float maxSpeed = radiusMaxSpeed > 0f ? radiusMaxSpeed : Mathf.Infinity;
        currentRadius = Mathf.SmoothDamp(
            currentRadius,
            destinationRadius,
            ref radiusVelocity,
            radiusSmoothTime,
            maxSpeed,
            Time.deltaTime);

        if (float.IsNaN(currentRadius))
        {
            currentRadius = destinationRadius;
            radiusVelocity = 0f;
            return;
        }

        ApplyRadius(currentRadius);
    }

    public void resizeMap(int totalCountdown)
    {
        destinationRadius = RadiusFromCountdown(totalCountdown);
    }

    public void snapToCountdown(int totalCountdown)
    {
        snapToRadius(RadiusFromCountdown(totalCountdown));
    }

    public void snapToRadius(float radius)
    {
        destinationRadius = Mathf.Max(0.01f, radius);
        currentRadius = destinationRadius;
        radiusVelocity = 0f;
        ApplyRadius(currentRadius);
    }

    public float GetRadius()
    {
        return currentRadius;
    }

    float GridLineThickness()
    {
        // The grid shader lights pixels within _LineWidth on both sides of a line,
        // so the visible thickness is twice that value.
        Material gridMaterial = Resources.Load<Material>("GridBackground");
        if (gridMaterial != null && gridMaterial.HasProperty("_LineWidth"))
            return gridMaterial.GetFloat("_LineWidth") * 2f;
        return lineWidth;
    }

    float RadiusFromCountdown(int totalCountdown)
    {
        return Mathf.Sqrt(Mathf.Max(0, totalCountdown)) * mapSize;
    }

    void ApplyRadius(float radius)
    {
        radius = Mathf.Max(0.01f, radius);
        circleCollider.radius = radius;

        Vector3 center = transform.position;
        for (int i = 0; i < segments; i++)
        {
            float angle = (i / (float)segments) * Mathf.PI * 2f;
            border.SetPosition(i, center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        Player player = other.GetComponent<Player>();
        if (player != null)
            player.setInBounds(false);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        Player player = other.GetComponent<Player>();
        if (player != null)
            player.setInBounds(true);
    }
}
