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
    bool displayFrozen;

    Sprite pendingOpSprite;
    Material pendingOpMaterial;

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
            pendingOpSprite = block.GetSymbolSprite();
            pendingOpMaterial = block.GetSymbolMaterial();
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
                    GridBackground.PulseFromChange(transform.position, before, countdown, default, GetInstanceID());
                    CameraShake.ShakeFromChange(before, countdown);
                }
                if (rate != rateBefore)
                    CameraShake.ShakeFromChange((float)rateBefore, (float)rate);
                operationFlash.PlayCombo(pendingOpSprite, pendingOpMaterial, block.GetSymbolSprite(), block.GetSymbolMaterial());
                pendingOpSprite = null;
                pendingOpMaterial = null;
                appliedOperation = "";
            } else { // attempted to apply an operation on top of an operation
                // red error effect
            }
        }
    }
}