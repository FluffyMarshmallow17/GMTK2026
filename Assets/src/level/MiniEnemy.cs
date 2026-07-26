using UnityEngine;
using TMPro;

public class MiniEnemy : MonoBehaviour
{
    const float GridPulseStrength = 0.5f;
    public float time;
    public float rate;
    private int countdown;
    private Transform playerTransform;
    public string appliedOperation;
    public int moveSpeed;
    public TextMeshPro display;
    public float countdownDisplaySmoothTime = 0.35f;    public float launchForce = 12f;
    public float launchDuration = 0.45f;
    public float launchStartScale = 0.2f;
    [Tooltip("Seconds to fade in when spawned.")]
    public float fadeInDuration = 0.3f;
    [Tooltip("Seconds to fade out when despawning.")]
    public float fadeOutDuration = 0.4f;

    Rigidbody2D rb;
    LevelManager levelManager;
    SmoothCountdownDisplay countdownDisplay = new SmoothCountdownDisplay();
    OperationFlash operationFlash = new OperationFlash();
    Vector3 normalScale;
    bool launching;
    float launchTimer;
    Vector2 launchDirection;

    enum FadeState { In, Alive, Out }
    FadeState fadeState;
    float fadeTimer;
    SpriteRenderer[] renderers;

    void Awake()
    {
        rate = 1;
        time = 0;
        rb = GetComponent<Rigidbody2D>();
        levelManager = FindAnyObjectByType<LevelManager>();
        normalScale = transform.localScale;
        // Cache the body sprites before OperationFlash adds its own renderer.
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            playerTransform = playerObject.transform;
        }
        countdownDisplay.Init(display, countdown, countdownDisplaySmoothTime);
        operationFlash.Init(display, transform);

        // Fade into existence on spawn.
        fadeState = FadeState.In;
        fadeTimer = 0f;
        SetAlpha(0f);
    }

    public float getRate()
    {
        return rate;
    }

    void Update()
    {
        if (operationFlash.IsActive)
            operationFlash.Update();
        else
            countdownDisplay.Update(Mathf.Max(0, countdown));

        UpdateFade();
    }

    void UpdateFade()
    {
        if (fadeState == FadeState.In)
        {
            fadeTimer += Time.deltaTime;
            float a = Mathf.Clamp01(fadeTimer / Mathf.Max(0.01f, fadeInDuration));
            SetAlpha(a);
            if (a >= 1f)
                fadeState = FadeState.Alive;
        }
        else if (fadeState == FadeState.Out)
        {
            fadeTimer += Time.deltaTime;
            float a = 1f - Mathf.Clamp01(fadeTimer / Mathf.Max(0.01f, fadeOutDuration));
            SetAlpha(a);
            if (a <= 0f)
                Destroy(gameObject);
        }
    }

    void SetAlpha(float a)
    {
        if (renderers != null)
            foreach (SpriteRenderer r in renderers)
            {
                if (r == null) continue;
                Color c = r.color;
                c.a = a;
                r.color = c;
            }
        if (display != null)
            display.alpha = a;
    }

    // Stop acting and fade out, then destroy (used for every despawn).
    void BeginFadeOut()
    {
        if (fadeState == FadeState.Out)
            return;
        fadeState = FadeState.Out;
        fadeTimer = 0f;
        if (rb != null)
            rb.linearVelocity = Vector2.zero;
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = false;
    }

    void FixedUpdate()
    {
        if (fadeState == FadeState.Out)
            return; // dying: just let the fade play out

        if (countdown <= 0)
        {
            BeginFadeOut();
            return;
        }

        if (launching)
        {
            launchTimer += Time.deltaTime;
            float t = Mathf.Clamp01(launchTimer / launchDuration);
            // Smooth ease-in-out for a nicer launch into existence.
            float eased = Mathf.SmoothStep(0f, 1f, t);
            transform.localScale = Vector3.Lerp(normalScale * launchStartScale, normalScale, eased);
            float speed = Mathf.Lerp(launchForce, moveSpeed, eased);
            rb.linearVelocity = launchDirection * speed;
            if (t >= 1f)
                launching = false;
            return;
        }

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

    public void LaunchFromBoss(Vector3 bossPosition)
    {
        normalScale = transform.localScale;
        transform.localScale = normalScale * launchStartScale;

        launchDirection = ((Vector2)(transform.position - bossPosition)).normalized;
        if (launchDirection == Vector2.zero)
            launchDirection = Random.insideUnitCircle.normalized;

        rb.linearVelocity = launchDirection * launchForce;
        launching = true;
        launchTimer = 0f;
    }

    public void setCountdown(int countdown)
    {
        this.countdown = countdown;
        countdownDisplay.Snap(countdown);
    }

    public int getCountdown()
    {
        return countdown;
    }


    public void decreaseCountdown()
    {
        countdown--;
        GridBackground.Pulse(transform.position, GridPulseStrength, rb != null ? rb.linearVelocity : Vector2.zero, GetInstanceID());
    }
    
    void OnDestroy()
    {
        if (levelManager != null)
            levelManager.UnregisterMiniEnemy(this);
    }

    void OnCollisionEnter2D(Collision2D other)
    {
        Block block = other.gameObject.GetComponentInParent<Block>();
        Player player = other.gameObject.GetComponentInParent<Player>();
        if (player != null)
        {
            int before = player.getCountdown();
            player.decreaseCountdown(countdown);
            CameraShake.ShakeFromChange(before, player.getCountdown());
            AudioManager.Instance.PlaySFX(SFX.Subtract);
            BeginFadeOut();
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
            } else if (int.TryParse(affect, out int number)) { // attempted to apply a number without an operation
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
                if (countdown != before)
                {
                    GridBackground.PulseFromChange(transform.position, before, countdown,
                        rb != null ? rb.linearVelocity : Vector2.zero, GetInstanceID(), GridPulseStrength);
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