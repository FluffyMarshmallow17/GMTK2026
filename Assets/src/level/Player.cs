using System.Collections.Generic;
using TMPro;
using Unity.IntegerTime;
using UnityEngine;

public class Player : MonoBehaviour
{
    public int countdown;
    private GameControls controls;
    public float moveSpeed = 5f;
    public float orbitalSpeed = 15f;
    public float shootForce = 1000f;
    public TextMeshPro display;

    public List<Block> inRange;
    public Block inConnection;
    public List<Block> blocks;
    public List<Block> absorbedBlocks;
    public Block selectedBlock;
    public double time;
    private double rate;
    bool inBounds;

    public string appliedOperation;


    public GameObject targetPrefab;
    public GameObject selectedPrefab;
    public float countdownDisplaySmoothTime = 0.35f;

    Rigidbody2D rb;
    SmoothCountdownDisplay countdownDisplay = new SmoothCountdownDisplay();
    OperationFlash operationFlash = new OperationFlash();
    bool displayFrozen;
    bool coasting;
    Vector2 coastVelocity;

    void Awake()
    {
        rate = 1;
        time = 0;
        inBounds = true;
        appliedOperation = "";
        controls = new GameControls();
        rb = GetComponent<Rigidbody2D>();
        inRange = new List<Block>();
        blocks = new List<Block>();
        absorbedBlocks = new List<Block>();
        countdownDisplay.Init(display, countdown, countdownDisplaySmoothTime);
        operationFlash.Init(display, transform);
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
        GridBackground.Pulse(transform.position, 1f, rb.linearVelocity, GetInstanceID());
    }

    public void decreaseCountdown(int amount)
    {
        int before = countdown;
        countdown -= amount;
        GridBackground.PulseFromChange(transform.position, before, countdown, rb.linearVelocity, GetInstanceID());
    }

    public void setCountdown(int countdown)
    {
        this.countdown = countdown;
    }

    public void SnapDisplay(int value)
    {
        countdownDisplay.Snap(value);
    }

    public void FreezeDisplay(int value)
    {
        displayFrozen = true;
        countdownDisplay.Snap(value);
    }

    public void UnfreezeDisplay()
    {
        displayFrozen = false;
    }

    /// <summary>Keep moving at the given velocity without WASD during level-end cinematic.</summary>
    public void BeginCoastMovement(Vector2 velocity)
    {
        coasting = true;
        coastVelocity = velocity;
        controls.Player.Disable();
    }

    void FixedUpdate()
    {
        blocks.RemoveAll(b => b == null);
        absorbedBlocks.RemoveAll(b => b == null);
        inRange.RemoveAll(b => b == null);
        if (inConnection == null)
            inConnection = null;

        if (coasting)
        {
            rb.linearVelocity = coastVelocity;
        }
        else
        {
            Vector2 movement = controls.Player.Move.ReadValue<Vector2>();

            if (movement.x != 0)
                rb.AddTorque(-movement.x * 0.5f);

            rb.linearVelocity = movement * moveSpeed;
        }

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
            int before = countdown;
            countdown = (int) ((double) countdown * 0.75);
            time = 0;
            GridBackground.PulseFromChange(transform.position, before, countdown, rb.linearVelocity, GetInstanceID());
        }
    }

    public double getRate()
    {
        return rate;
    }

    void Update()
    {
        if (operationFlash.IsActive)
            operationFlash.Update();
        else if (!displayFrozen)
            countdownDisplay.Update(countdown);
        if (controls.Player.TakeIn.WasPressedThisFrame())
        {
            if (inConnection)
            {
                blocks.Add(inConnection);
                inRange.Remove(inConnection);
                Transform target = inConnection.transform.Find("Target(Clone)");
                if (target != null)
                    Destroy(target.gameObject);
                    // decide which pickup sound based on the block's affect
                    if (int.TryParse(inConnection.getAffect(), out _))
                        AudioManager.Instance.PlaySFX(SFX.PickupBlock);
                    else
                        AudioManager.Instance.PlaySFX(SFX.PickupOperator);

                inConnection = null;
            }
        }

        if (controls.Player.Absorb.WasPressedThisFrame())
        {
            if (selectedBlock)
            {
                RemoveMarker(selectedBlock, selectedPrefab);
                absorbedBlocks.Add(selectedBlock);
                blocks.Remove(selectedBlock);
                inRange.Remove(selectedBlock);
                selectedBlock = null;
            }
        }

        if (controls.Player.RotatePossible.WasPressedThisFrame())
        {
            RotateConnection();
        }

        if (controls.Player.RotateInventory.WasPressedThisFrame())
        {
            RotateSelection();
        }

        if(controls.Player.Shoot.WasPressedThisFrame())
        {
            if (selectedBlock)
            {
                RemoveMarker(selectedBlock, selectedPrefab);
                Vector2 center = transform.position;
                Vector2 shootDirection = ((Vector2)selectedBlock.transform.position - center).normalized;
                selectedBlock.GetComponent<Rigidbody2D>().AddForce(shootDirection * shootForce);
                blocks.Remove(selectedBlock);
                selectedBlock = null;
                AudioManager.Instance.PlaySFX(SFX.Push);
            }
        }

    }

    // Rotates the in-range connection target through all pickable blocks around the player.
    void RotateConnection()
    {
        List<Block> candidates = new List<Block>();
        foreach (Block block in inRange)
        {
            if (block != null && !blocks.Contains(block) && !absorbedBlocks.Contains(block))
                candidates.Add(block);
        }
        if (inConnection != null)
            candidates.Add(inConnection);
        if (candidates.Count == 0)
            return;

        Block next = NextBlockInRotation(candidates, inConnection);
        if (next == null || next == inConnection)
            return;

        if (inConnection != null)
        {
            RemoveMarker(inConnection, targetPrefab);
            // Hand the old connection back to the in-range pool (FixedUpdate removes the
            // active connection from inRange, so re-add it to keep it cycleable).
            if (!inRange.Contains(inConnection))
                inRange.Add(inConnection);
        }

        inRange.Remove(next);
        inConnection = next;
        Instantiate(targetPrefab, next.transform.position, Quaternion.identity, next.transform);

    }

    // Rotates the selection (used by Absorb/Shoot) through the blocks held in orbit.
    void RotateSelection()
    {
        if (blocks.Count == 0)
            return;

        Block next = NextBlockInRotation(blocks, selectedBlock);
        if (next == null || next == selectedBlock)
            return;

        if (selectedBlock != null)
            RemoveMarker(selectedBlock, selectedPrefab);

        selectedBlock = next;
        GameObject selectMarker = Instantiate(selectedPrefab, next.transform.position, Quaternion.identity, next.transform);
        selectMarker.AddComponent<SelectZoom>();
        AudioManager.Instance.PlaySFX(SFX.Target);
    }

    // Blocks are ordered by a full sweep around the player: across the top from right
    // to left, then across the bottom from left to right, then wrapping around.
    // Returns the candidate that comes after `current` in that sweep (the one with the
    // smallest positive angular step). With no current block, starts at the sweep origin.
    Block NextBlockInRotation(List<Block> candidates, Block current)
    {
        Block best = null;
        float bestDelta = float.MaxValue;

        if (current == null)
        {
            foreach (Block candidate in candidates)
            {
                if (candidate == null)
                    continue;
                float angle = SweepAngle(candidate);
                if (angle < bestDelta)
                {
                    bestDelta = angle;
                    best = candidate;
                }
            }
            return best;
        }

        float currentAngle = SweepAngle(current);
        foreach (Block candidate in candidates)
        {
            if (candidate == null || candidate == current)
                continue;
            float delta = Mathf.Repeat(SweepAngle(candidate) - currentAngle, 360f);
            if (delta <= 0f)
                delta += 360f;
            if (delta < bestDelta)
            {
                bestDelta = delta;
                best = candidate;
            }
        }
        return best;
    }

    // Angle of the block around the player in [0, 360), increasing in sweep order.
    float SweepAngle(Block block)
    {
        Vector2 offset = block.transform.position - transform.position;
        return Mathf.Repeat(Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg, 360f);
    }

    void RemoveMarker(Block block, GameObject markerPrefab)
    {
        if (block == null || markerPrefab == null)
            return;
        Transform marker = block.transform.Find(markerPrefab.name + "(Clone)");
        if (marker != null)
            Destroy(marker.gameObject);
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
            } else if (string.Equals("decay", affect)) {
                appliedOperation = "decay";
            } else if (string.Equals("grow", affect)) {
                appliedOperation = "grow";
            } else { // attempted to apply a number without an operation
                // red error effect
                return;
            }
            FlashOperation(block);
        } else {
            if (int.TryParse(affect, out int number)) {
                int before = countdown;
                double rateBefore = rate;
                if (string.Equals("+", appliedOperation)) {
                    countdown += number;
                    AudioManager.Instance.PlaySFX(SFX.Add);
                } else if (string.Equals("-", appliedOperation)) {
                    countdown -= number;
                    AudioManager.Instance.PlaySFX(SFX.Subtract);
                } else if (string.Equals("x", appliedOperation)) {
                    countdown *= number;
                    AudioManager.Instance.PlaySFX(SFX.Multiply);
                } else if (string.Equals("/", appliedOperation)) {
                    countdown /= number;
                    AudioManager.Instance.PlaySFX(SFX.Divide);
                } else if (string.Equals("decay", appliedOperation)) {
                    rate /= number;
                } else if (string.Equals("grow", appliedOperation)) {
                    rate *= number;
                }
                if (countdown != before)
                {
                    GridBackground.PulseFromChange(transform.position, before, countdown, rb.linearVelocity, GetInstanceID());
                    CameraShake.ShakeFromChange(before, countdown);
                }
                if (rate != rateBefore)
                    CameraShake.ShakeFromChange((float)rateBefore, (float)rate);
                appliedOperation = "";
            } else { // attempted to apply an operation on top of an operation
                // red error effect
            }
        }
    }

    void FlashOperation(Block block)
    {
        operationFlash.Play(block.GetSymbolSprite(), block.GetSymbolMaterial());
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