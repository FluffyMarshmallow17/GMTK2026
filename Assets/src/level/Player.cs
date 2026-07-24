using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    public int countdown;
    private GameControls controls;
    public float moveSpeed = 5f;
    public TextMeshPro display;

    public List<Block> inRange;
    public Block inConnection;
    public List<Block> blocks;
    public List<Block> absorbedBlocks;

    public string appliedOperation;


    public GameObject targetPrefab;
    

    Rigidbody2D rb;

     

    void Awake()
    {
        appliedOperation = "";
        controls = new GameControls();
        countdown = 100;
        rb = GetComponent<Rigidbody2D>();
        inRange = new List<Block>();
        blocks = new List<Block>();
        absorbedBlocks = new List<Block>();
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
        blocks.RemoveAll(b => b == null);
        absorbedBlocks.RemoveAll(b => b == null);
        inRange.RemoveAll(b => b == null);
        if (inConnection == null)
            inConnection = null;

        display.text = "" + countdown;
        Vector2 movement = controls.Player.Move.ReadValue<Vector2>();

        if (movement.x != 0)
        {
            rb.AddTorque(-movement.x * 0.5f);
        }

        rb.linearVelocity = movement * moveSpeed;

        if (controls.Player.TakeIn.WasPressedThisFrame())
        {
            Debug.Log("reading this");
            if (inConnection)
            {
                blocks.Add(inConnection);
                inRange.Remove(inConnection);
                Transform target = inConnection.transform.Find("Target(Clone)");
                if (target != null)
                    Destroy(target.gameObject);
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
        Debug.Log("Count is: " + blocks.Count);
        // Debug.Log(countdown);

        if (controls.Player.Absorb.WasPressedThisFrame())
        {
            Block absorbed = blocks[0];
            absorbedBlocks.Add(absorbed);
            blocks.Remove(absorbed);
            inRange.Remove(absorbed);
        }

        foreach (Block block in absorbedBlocks)
        {
            Vector2 direction = transform.position - block.transform.position;
            float strength = 1 / direction.magnitude;
            block.GetComponent<Rigidbody2D>().AddForce(direction.normalized * 50 * strength);

            float t = Mathf.Clamp01(direction.magnitude / 4); // 4 is hardcoded radius
            float scale = Mathf.Lerp(0.1f, 1f, t);
            block.transform.localScale = Vector3.one * scale;
        }

        if (inConnection == null)
        {
            for (int i = 0; i < inRange.Count; i++)
            {
                Block candidate = inRange[i];
                if (candidate == null || blocks.Contains(candidate) || absorbedBlocks.Contains(candidate))
                    continue;

                Instantiate(targetPrefab, candidate.transform.position, Quaternion.identity, candidate.transform);
                inConnection = candidate;
                inRange.RemoveAt(i);
                break;
            }
        }
    }

    void OnCollisionEnter2D(Collision2D other)
    {
        Block block = other.gameObject.GetComponentInParent<Block>();

        if (block != null && absorbedBlocks.Contains(block))
        {
            absorbedBlocks.Remove(block);
            blocks.Remove(block);
            inRange.Remove(block);
            applyAffect(block);

            Destroy(other.gameObject);
        }
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

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<Rigidbody2D>() == null)
            return;

        Block block = other.GetComponentInParent<Block>();
        if (block == null || blocks.Contains(block) || absorbedBlocks.Contains(block) || inRange.Contains(block))
            return;

        inRange.Add(block);

        if (inConnection == null && !absorbedBlocks.Contains(block))
        {
            Instantiate(targetPrefab, block.transform.position, Quaternion.identity, block.transform);
            inConnection = block;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponent<Rigidbody2D>() == null)
            return;

        Block block = other.GetComponentInParent<Block>();
        if (block == null)
            return;

        inRange.Remove(block);

        if (inConnection == block)
        {
            Transform target = block.transform.Find("Target(Clone)");
            if (target != null)
                Destroy(target.gameObject);
            inConnection = null;
        }
    }
}