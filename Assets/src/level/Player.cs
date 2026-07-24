using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    private int countdown;
    private GameControls controls;
    public float moveSpeed = 5f;
    public TextMeshPro display;

    public List<Block> inRange;
    public Block inConnection;

    public GameObject targetPrefab;
    

    public List<Block> blocks;
    Rigidbody2D rb;

     

    void Awake()
    {
        controls = new GameControls();
        countdown = 100;
        rb = GetComponent<Rigidbody2D>();
    }

    void LateUpdate()
    {
        display.transform.rotation = Quaternion.identity;
    }

    private void OnEnable()
    {
        controls.Player.Enable();
    }

    private void OnDisable()
    {
        controls.Player.Disable();
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
        Vector2 movement = controls.Player.Move.ReadValue<Vector2>();
        rb.linearVelocity = movement * moveSpeed;

        if (controls.Player.TakeIn.IsPressed())
        {
            Debug.Log("reading this");
            if (inConnection)
            {
                blocks.Add(inConnection);
                inRange.Remove(inConnection);
                Destroy(inConnection.transform.Find("Target(Clone)").gameObject);
                inConnection = null;
            }
        }

        foreach (Block block in blocks)
        {

            CircleCollider2D field = transform.Find("Field").GetComponent<CircleCollider2D>();

            Vector2 center = field.bounds.center;
            float radius = field.radius * transform.lossyScale.x;

            Vector2 fromCenter = ((Vector2)block.transform.position - center).normalized;
            Vector2 edge = center + fromCenter * radius;

            Vector2 direction = edge - (Vector2)block.transform.position;
            float distance = direction.magnitude;

            block.GetComponent<Rigidbody2D>().AddForce(direction.normalized * distance * 10);
        }
        // Debug.Log(countdown);


    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Rigidbody2D rb = other.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            inRange.Add(other.GetComponentInParent<Block>());
            if (inConnection == null && other.CompareTag("Block") && !blocks.Contains(other.GetComponentInParent<Block>()))
            {
                Instantiate(targetPrefab, other.transform.position, Quaternion.identity, other.transform);
                inConnection = other.GetComponentInParent<Block>();
            }
            inRange.Add(other.GetComponentInParent<Block>());
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        Rigidbody2D rb = other.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            inRange.Remove(other.GetComponentInParent<Block>());
            Transform temp = other.transform.Find("Target(Clone)"); // beware of this tiny issue
            if (temp != null)
            {
                Destroy(temp.gameObject);
                inConnection = null;
            }
        }
    }
}