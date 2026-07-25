using UnityEngine;
using TMPro;

public class MiniEnemy : MonoBehaviour
{
    private int countdown;
    private Transform playerTransform;
    public string appliedOperation;
    public int moveSpeed;
    public TextMeshPro display;
    public float countdownDisplaySmoothTime = 0.35f;

    Rigidbody2D rb;
    SmoothCountdownDisplay countdownDisplay = new SmoothCountdownDisplay();

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            playerTransform = playerObject.transform;
        }
        countdownDisplay.Init(display, countdown, countdownDisplaySmoothTime);
    }

    void Update()
    {
        countdownDisplay.Update(countdown);
    }

    void FixedUpdate()
    {
        if (countdown <= 0)
        {
            Destroy(gameObject);
        }

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

    public void setCountdown(int countdown)
    {
        this.countdown = countdown;
        countdownDisplay.Snap(countdown);
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