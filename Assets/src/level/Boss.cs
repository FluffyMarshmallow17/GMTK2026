using UnityEngine;
using TMPro;

public class Boss : MonoBehaviour
{
    private int countdown;
    private SpriteRenderer sr;
    private Material material;
    public TextMeshPro display;
    public string appliedOperation;
    public float countdownDisplaySmoothTime = 0.35f;
    public double rate;

    SmoothCountdownDisplay countdownDisplay = new SmoothCountdownDisplay();
    OperationFlash operationFlash = new OperationFlash();

    void Awake()
    {
        rate = 1;
        appliedOperation = "";
        countdown = 100;
        sr = transform.Find("Sprite").GetComponent<SpriteRenderer>();
        material = sr.material;
        countdownDisplay.Init(display, countdown, countdownDisplaySmoothTime);
        operationFlash.Init(display, transform);
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
        GridBackground.Pulse(transform.position);
    }

    public void decreaseCountdown(int countdown)
    {
        int before = this.countdown;
        this.countdown -= countdown;
        GridBackground.PulseFromChange(transform.position, before, this.countdown);
    }

    public double getRate()
    {
        return rate;
    }

    void Update()
    {
        if (operationFlash.IsActive)
            operationFlash.Update();
        else
            countdownDisplay.Update(countdown);
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
            } else if (string.Equals("decay", affect)) {
                appliedOperation = "decay";
            } else if (string.Equals("grow", affect)) {
                appliedOperation = "grow";
            } else { // attempted to apply a number without an operation
                // red error effect
                return;
            }
            operationFlash.Play(block.GetSymbolSprite(), block.GetSymbolMaterial());
        } else {
            if (int.TryParse(affect, out int number)) {
                int before = countdown;
                if (string.Equals("+", appliedOperation)) {
                    countdown += number;
                } else if (string.Equals("-", appliedOperation)) {
                    countdown -= number;
                } else if (string.Equals("x", appliedOperation)) {
                    countdown *= number;
                } else if (string.Equals("/", appliedOperation)) {
                    countdown /= number;
                } else if (string.Equals("decay", appliedOperation)) {
                    rate /= number;
                } else if (string.Equals("grow", appliedOperation)) {
                    rate *= number;
                }
                if (countdown != before)
                {
                    GridBackground.PulseFromChange(transform.position, before, countdown);
                    CameraShake.ShakeFromChange(before, countdown);
                }
                appliedOperation = "";
            } else { // attempted to apply an operation on top of an operation
                // red error effect
            }
        }
    }
}