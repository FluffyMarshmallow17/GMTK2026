using UnityEngine;

public class Map : MonoBehaviour
{
    public float mapSize = 1f;
    public int segments = 64;
    public float lineWidth = 0.15f;
    public Color lineColor = Color.white;
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
        
        border = GetComponent<LineRenderer>();

        border.loop = true;
        border.useWorldSpace = true;
        border.widthMultiplier = lineWidth;
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
        destinationRadius = RadiusFromCountdown(totalCountdown);
        currentRadius = destinationRadius;
        radiusVelocity = 0f;
        ApplyRadius(currentRadius);
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
