using UnityEngine;
using TMPro;

public class MiniEnemy : MonoBehaviour
{
    private int countdown;
    private Transform playerTransform;
    public string appliedOperation;
    public int moveSpeed;
    public TextMeshPro display;


    Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
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