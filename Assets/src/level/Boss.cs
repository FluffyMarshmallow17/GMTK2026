using UnityEngine;
using TMPro;

public class Boss : MonoBehaviour
{
    public const int MaxCountdown = 9999;

    private int countdown;
    private SpriteRenderer sr;
    private Material material;
    public TextMeshPro display;
    public string appliedOperation;
    public float countdownDisplaySmoothTime = 0.35f;
    public double rate;

    [Header("Spin")]
    [Tooltip("Fastest the boss body spins, degrees/second.")]
    public float maxSpinSpeed = 120f;
    [Tooltip("How quickly the spin speed ramps toward a new random target, deg/sec^2.")]
    public float spinAcceleration = 90f;
    [Tooltip("Seconds between picking a new random spin speed.")]
    public float spinChangeInterval = 2.5f;

    SmoothCountdownDisplay countdownDisplay = new SmoothCountdownDisplay();
    OperationFlash operationFlash = new OperationFlash();
    bool displayFrozen;

    float currentSpinSpeed;
    float targetSpinSpeed;
    float spinTimer;

    Rigidbody2D rb;
    Vector3 startPosition;

    void Awake()
    {
        rate = 1;
        appliedOperation = "";
        countdown = 100;
        sr = transform.Find("Sprite").GetComponent<SpriteRenderer>();
        material = sr.material;
        countdownDisplay.Init(display, countdown, countdownDisplaySmoothTime);
        operationFlash.Init(display, transform);

        // Boss is stuck in place (block hits can't push it); only the sprite spins.
        startPosition = transform.position;
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.constraints = RigidbodyConstraints2D.FreezeAll;
    }

    void LateUpdate()
    {
        // Hard-pin the position in case anything still nudges it.
        transform.position = startPosition;
    }

    public void setCountdown(int countdown)
    {
        this.countdown = Mathf.Min(countdown, MaxCountdown);
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

    public int getCountdown()
    {
        return countdown;
    }


    public void decreaseCountdown()
    {
        countdown--;
        GridBackground.Pulse(transform.position, 1f, default, GetInstanceID());
    }

    public void decreaseCountdown(int countdown)
    {
        int before = this.countdown;
        this.countdown -= countdown;
        GridBackground.PulseFromChange(transform.position, before, this.countdown, default, GetInstanceID());
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
            countdownDisplay.Update(Mathf.Max(0, countdown)); // never show negatives before the cinematic

        Spin();
    }

    // Stays in place but slowly spins the body, drifting toward new random spin
    // speeds. Only the sprite spins, so the collider and the number stay put.
    void Spin()
    {
        if (sr == null)
            return;

        spinTimer += Time.deltaTime;
        if (spinTimer >= spinChangeInterval)
        {
            spinTimer = 0f;
            targetSpinSpeed = Random.Range(-maxSpinSpeed, maxSpinSpeed);
        }
        currentSpinSpeed = Mathf.MoveTowards(currentSpinSpeed, targetSpinSpeed, spinAcceleration * Time.deltaTime);
        sr.transform.Rotate(0f, 0f, currentSpinSpeed * Time.deltaTime);
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
                operationFlash.Play(block.GetSymbolSprite(), block.GetSymbolMaterial());
            } else if (string.Equals("-", affect)) {
                appliedOperation = "-";
                operationFlash.Play(block.GetSymbolSprite(), block.GetSymbolMaterial());
            } else if (string.Equals("x", affect)) {
                appliedOperation = "x";
                operationFlash.Play(block.GetSymbolSprite(), block.GetSymbolMaterial());
            } else if (string.Equals("/", affect)) {
                appliedOperation = "/";
                operationFlash.Play(block.GetSymbolSprite(), block.GetSymbolMaterial());
            } else if (string.Equals("decay", affect)) {
                appliedOperation = "decay";
                operationFlash.Play(block.GetSymbolSprite(), block.GetSymbolMaterial());
            } else if (string.Equals("grow", affect)) {
                appliedOperation = "grow";
                operationFlash.Play(block.GetSymbolSprite(), block.GetSymbolMaterial());
            } else if (int.TryParse(affect, out int number)) { // number without an operation — no flash
                countdown -= number;
                AudioManager.Instance.PlaySFX(SFX.Subtract);
            } else {
                return;
            }
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
                countdown = Mathf.Min(countdown, MaxCountdown);
                if (countdown != before)
                {
                    GridBackground.PulseFromChange(transform.position, before, countdown, default, GetInstanceID());
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
}