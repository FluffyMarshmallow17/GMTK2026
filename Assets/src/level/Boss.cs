using UnityEditor;
using UnityEngine;
using TMPro;
public class Boss : MonoBehaviour
{
    private int countdown;
    private SpriteRenderer sr;
    private Material material;
    public TextMeshPro display;
    public string appliedOperation;

    void Awake()
    {
        appliedOperation = "";
        countdown = 500;
        sr = transform.Find("Sprite").GetComponent<SpriteRenderer>();
        material = sr.material;
    }

    public int getCountdown()
    {
        return countdown;
    }


    public void decreaseCountdown()
    {
        countdown--;
    }

    void Update()
    {
        display.text = "" + countdown;
    }

    void OnCollisionEnter2D(Collision2D other)
    {
        Block block = other.gameObject.GetComponentInParent<Block>();
        if (block == null)
            return;

        applyAffect(block);
        Destroy(other.gameObject);
        material.SetFloat("GlowAmount", 2);
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