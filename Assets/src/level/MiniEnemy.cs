using UnityEngine;
using TMPro;

public class MiniEnemy : MonoBehaviour
{
    private int countdown;
    private Transform playerTransform;
    public string appliedOperation;
    public int moveSpeed;
    public TextMeshPro display;
    public float launchForce = 12f;
    public float launchDuration = 0.45f;
    public float launchStartScale = 0.2f;

    Rigidbody2D rb;
    Vector3 normalScale;
    bool launching;
    float launchTimer;
    Vector2 launchDirection;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        normalScale = transform.localScale;
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            playerTransform = playerObject.transform;
        }
    }

    void Update()
    {
        if (countdown <= 0)
        {
            Destroy(gameObject);
        }
        display.text = "" + countdown;

        if (launching)
        {
            launchTimer += Time.deltaTime;
            float t = Mathf.Clamp01(launchTimer / launchDuration);
            // Ease out: fast shove that quickly settles to normal speed
            float eased = 1f - (1f - t) * (1f - t);
            transform.localScale = Vector3.Lerp(normalScale * launchStartScale, normalScale, eased);
            float speed = Mathf.Lerp(launchForce, moveSpeed, eased);
            rb.linearVelocity = launchDirection * speed;
            if (t >= 1f)
                launching = false;
            return;
        }

        print("Player Transform: " + playerTransform);
        if (playerTransform != null)
        {
            Vector2 direction = (playerTransform.position - transform.position).normalized;
            rb.linearVelocity = direction * moveSpeed;
            if (direction != Vector2.zero)
            {
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                rb.rotation = angle - 90f; // Adjust the rotation to face the player
            }
        }
    }

    public void LaunchFromBoss(Vector3 bossPosition)
    {
        normalScale = transform.localScale;
        transform.localScale = normalScale * launchStartScale;

        launchDirection = ((Vector2)(transform.position - bossPosition)).normalized;
        if (launchDirection == Vector2.zero)
            launchDirection = Random.insideUnitCircle.normalized;

        rb.linearVelocity = launchDirection * launchForce;
        launching = true;
        launchTimer = 0f;
    }

    public void setCountdown(int countdown)
    {
        this.countdown = countdown;
    }

    public int getCountdown()
    {
        return countdown;
    }


    public void decreaseCountdown()
    {
        countdown--;
    }
    
    void OnCollisionEnter2D(Collision2D other)
    {
        Block block = other.gameObject.GetComponentInParent<Block>();
        Player player = other.gameObject.GetComponentInParent<Player>();
        if (player != null)
        {
            player.decreaseCountdown(countdown);
            Destroy(gameObject);
        }
        if (block != null)
        {
            applyAffect(block);
            Destroy(other.gameObject);
        }
            
        //material.SetFloat("GlowAmount", 2);
    }

    private void applyAffect(Block block)
    {
        string affect = block.getAffect();
        if (string.IsNullOrEmpty(appliedOperation)) {
            if (string.Equals("+", affect)) {
                appliedOperation = "+";
            } else if (string.Equals("-", affect)) {
                appliedOperation = "-";
            } else if (string.Equals("x", affect)) {
                appliedOperation = "x";
            } else if (string.Equals("/", affect)) {
                appliedOperation = "/";
            } else { // attempted to apply a number without an operation
                // red error effect
            }
        } else {
            if (int.TryParse(affect, out int number)) {
                if (string.Equals("+", appliedOperation)) {
                    countdown += number;
                } else if (string.Equals("-", appliedOperation)) {
                    countdown -= number;
                } else if (string.Equals("x", appliedOperation)) {
                    countdown *= number;
                } else if (string.Equals("/", appliedOperation)) {
                    countdown /= number;
                } 
                appliedOperation = "";
            } else { // attempted to apply an operation on top of an operation
                // red error effect
            }
        }
    }


}