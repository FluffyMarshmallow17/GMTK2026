using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class Player : MonoBehaviour
{
    public int countdown;
    private GameControls controls;
    public float moveSpeed = 5f;
    public float orbitalSpeed = 5f;
    public float shootForce = 1000f;
    public TextMeshPro display;

    public List<Block> inRange;
    public Block inConnection;
    public List<Block> blocks;
    public List<Block> absorbedBlocks;
    public Block selectedBlock;
    public double time;
    bool inBounds;

    public string appliedOperation;


    public GameObject targetPrefab;
    public GameObject selectedPrefab;
    public float countdownDisplaySmoothTime = 0.35f;

    Rigidbody2D rb;
    SmoothCountdownDisplay countdownDisplay = new SmoothCountdownDisplay();

    void Awake()
    {
        time = 0;
        inBounds = true;
        appliedOperation = "";
        controls = new GameControls();
        countdown = 20;
        rb = GetComponent<Rigidbody2D>();
        inRange = new List<Block>();
        blocks = new List<Block>();
        absorbedBlocks = new List<Block>();
        countdownDisplay.Init(display, countdown, countdownDisplaySmoothTime);
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

    public void decreaseCountdown(int amount)
    {
        countdown -= amount;
    }

    void FixedUpdate()
    {
        blocks.RemoveAll(b => b == null);
        absorbedBlocks.RemoveAll(b => b == null);
        inRange.RemoveAll(b => b == null);
        if (inConnection == null)
            inConnection = null;

        Vector2 movement = controls.Player.Move.ReadValue<Vector2>();

        if (movement.x != 0)
        {
            rb.AddTorque(-movement.x * 0.5f);
        }

        rb.linearVelocity = movement * moveSpeed;

        foreach (Block block in blocks)
        {

            CircleCollider2D field = transform.Find("Field").GetComponent<CircleCollider2D>();

            Vector2 center = field.bounds.center;
            float radius = field.radius * transform.lossyScale.x;

            Vector2 fromCenter = ((Vector2)block.transform.position - center).normalized;
            Vector2 edge = center + fromCenter * radius;

            Vector2 direction = edge - (Vector2)block.transform.position;
            Vector2 tangentialDirection = Vector2.Perpendicular(fromCenter).normalized;
            float distance = direction.magnitude;

            // In Radius
            block.GetComponent<Rigidbody2D>().AddForce(direction.normalized * distance * 20);
            
            // Orbitting
            block.GetComponent<Rigidbody2D>().AddForce(tangentialDirection * orbitalSpeed);
        }
        // Debug.Log("Count is: " + blocks.Count);
        // Debug.Log(countdown);

        foreach (Block block in absorbedBlocks)
        {
            Vector2 direction = transform.position - block.transform.position;
            float strength = 1 / direction.magnitude;
            block.GetComponent<Rigidbody2D>().AddForce(direction.normalized * 100 * strength);

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
        time += Time.fixedDeltaTime;
        if (!inBounds && time >= 1)
        {
            countdown = (int) ((double) countdown * 0.75);
            time = 0;
        }
    }

    void Update()
    {
        countdownDisplay.Update(countdown);
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

        if (controls.Player.Absorb.WasPressedThisFrame())
        {
            Block absorbed = blocks[0];
            absorbedBlocks.Add(absorbed);
            blocks.Remove(absorbed);
            inRange.Remove(absorbed);
        }

        if (controls.Player.Select.WasPressedThisFrame())
        {
            if (!selectedBlock)
            {
                selectedBlock = blocks[0];
            }
            if (selectedBlock)
            {
                Transform selectedTarget = selectedBlock.transform.Find("Target(Clone)");
                if (selectedTarget != null)
                    Destroy(selectedTarget.gameObject);
                int index = blocks.IndexOf(selectedBlock);
                index = (index + 1) % blocks.Count;
                selectedBlock = blocks[index];
                Instantiate(selectedPrefab, selectedBlock.transform.position, Quaternion.identity, selectedBlock.transform);
            }
        }

        if(controls.Player.Shoot.WasPressedThisFrame())
        {
            if (selectedBlock)
            {
                Transform selectedTarget = selectedBlock.transform.Find("Target(Clone)");
                if (selectedTarget != null)
                    Destroy(selectedTarget.gameObject);
                Vector2 center = transform.position;
                Vector2 shootDirection = ((Vector2)selectedBlock.transform.position - center).normalized;
                selectedBlock.GetComponent<Rigidbody2D>().AddForce(shootDirection * shootForce);
                blocks.Remove(selectedBlock);
                selectedBlock = null;
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

    public void setInBounds(bool inBounds)
    {
        this.inBounds = inBounds;
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