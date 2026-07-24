using UnityEditor;
using UnityEngine;
using TMPro;
public class Boss : MonoBehaviour
{
    private int countdown;
    private SpriteRenderer sr;
    private Material material;
    public TextMeshPro display;

    void Awake()
    {
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

        countdown = block.applyAffect(countdown);
        Destroy(other.gameObject);
        material.SetFloat("GlowAmount", 2);
    }

}