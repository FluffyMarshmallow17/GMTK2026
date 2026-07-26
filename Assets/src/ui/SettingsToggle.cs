using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// A world-space settings widget built from prefabs (so they can use glow
// materials, particles, etc). Put this on ONE empty GameObject and assign the
// gear / mute / quit prefabs — each should contain a SpriteRenderer.
// Click the gear: it spins, then reveals the mute + quit buttons. Click again to
// spin back and hide them.
//   - Mute toggles ALL audio (via AudioListener) and can swap its own sprite.
//   - Quit exits the game.
public class SettingsToggle : MonoBehaviour
{
    [Header("Prefabs (each should contain a SpriteRenderer — add a glow material for glow)")]
    public GameObject gearPrefab;
    public GameObject mutePrefab;
    public GameObject quitPrefab;

    [Header("Mute state sprites (optional — swapped on the mute prefab's SpriteRenderer)")]
    [Tooltip("Shown while audio is ON.")]
    public Sprite unmutedSprite;
    [Tooltip("Shown while audio is OFF.")]
    public Sprite mutedSprite;

    [Header("Placement (local offsets from this object)")]
    public Vector2 muteOffset = new Vector2(0f, -1.6f);
    public Vector2 quitOffset = new Vector2(0f, -3.1f);
    [Tooltip("Multiplies the button prefab's own scale (1 = as authored).")]
    public float buttonScale = 1f;

    [Header("Spin")]
    public float spinDuration = 0.4f;
    public float spinDegrees = 360f;

    [Header("Reveal")]
    [Tooltip("Pop-in/out time for the buttons.")]
    public float popDuration = 0.18f;

    [Tooltip("Logs every click to the Console to diagnose hit testing.")]
    public bool debugClicks = false;

    const string MuteKey = "audio_muted";

    GameObject gearObj, muteObj, quitObj;
    SpriteRenderer muteRenderer;
    Vector3 muteBaseScale = Vector3.one, quitBaseScale = Vector3.one;
    bool open, animating, muted;

    void Awake()
    {
        gearObj = Spawn(gearPrefab, Vector3.zero);
        muteObj = Spawn(mutePrefab, muteOffset);
        quitObj = Spawn(quitPrefab, quitOffset);

        if (muteObj != null)
        {
            muteRenderer = muteObj.GetComponentInChildren<SpriteRenderer>();
            muteBaseScale = muteObj.transform.localScale * buttonScale;
            muteObj.SetActive(false);
        }
        if (quitObj != null)
        {
            quitBaseScale = quitObj.transform.localScale * buttonScale;
            quitObj.SetActive(false);
        }

        muted = PlayerPrefs.GetInt(MuteKey, 0) == 1;
        AudioListener.volume = muted ? 0f : 1f;
        UpdateMuteSprite();
    }

    void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
            return;

        Camera cam = Camera.main;
        if (cam == null)
        {
            if (debugClicks) Debug.LogWarning("SettingsToggle: no Camera.main found — tag your menu camera 'MainCamera'.");
            return;
        }

        Vector3 world = cam.ScreenToWorldPoint(mouse.position.ReadValue());
        Collider2D[] hits = Physics2D.OverlapPointAll(world);

        if (debugClicks)
        {
            string names = hits.Length == 0 ? "nothing" : string.Join(", ", System.Array.ConvertAll(hits, h => h.name));
            Debug.Log($"SettingsToggle click at {(Vector2)world} — hit: {names}.");
        }

        foreach (Collider2D hit in hits)
        {
            if (IsPartOf(hit, gearObj)) { ToggleOpen(); return; }
            if (open && IsPartOf(hit, muteObj)) { ToggleMute(); return; }
            if (open && IsPartOf(hit, quitObj)) { QuitGame(); return; }
        }
    }

    // --- Build ----------------------------------------------------------------

    // Instantiates a prefab as a child at the given local offset and makes sure it
    // has a collider (sized to its SpriteRenderer) so it can be clicked.
    GameObject Spawn(GameObject prefab, Vector3 localPos)
    {
        if (prefab == null)
            return null;

        GameObject instance = Instantiate(prefab, transform);
        instance.transform.localPosition = localPos;
        EnsureCollider(instance);
        return instance;
    }

    static void EnsureCollider(GameObject instance)
    {
        SpriteRenderer sr = instance.GetComponentInChildren<SpriteRenderer>();
        GameObject host = sr != null ? sr.gameObject : instance;
        if (host.GetComponent<Collider2D>() != null)
            return;

        var col = host.AddComponent<BoxCollider2D>();
        if (sr != null && sr.sprite != null)
        {
            // bounds.center accounts for the sprite's pivot.
            col.size = sr.sprite.bounds.size;
            col.offset = sr.sprite.bounds.center;
        }
    }

    static bool IsPartOf(Collider2D hit, GameObject root)
    {
        return root != null && (hit.transform == root.transform || hit.transform.IsChildOf(root.transform));
    }

    // --- Open / close ---------------------------------------------------------

    void ToggleOpen()
    {
        if (animating || gearObj == null)
            return;
        open = !open;
        StartCoroutine(SpinRoutine(open));
    }

    IEnumerator SpinRoutine(bool opening)
    {
        animating = true;
        if (opening)
        {
            yield return Spin(spinDegrees);
            yield return Reveal(true);
        }
        else
        {
            yield return Reveal(false);
            yield return Spin(-spinDegrees);
        }
        animating = false;
    }

    IEnumerator Spin(float degrees)
    {
        Transform t = gearObj.transform;
        float start = t.eulerAngles.z;
        float end = start - degrees; // negative = clockwise
        float elapsed = 0f;
        while (elapsed < spinDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / spinDuration));
            t.rotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(start, end, k));
            yield return null;
        }
        t.rotation = Quaternion.Euler(0f, 0f, end);
    }

    IEnumerator Reveal(bool show)
    {
        if (show)
            SetButtonsActive(true);

        float elapsed = 0f;
        while (elapsed < popDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(elapsed / popDuration);
            ApplyScale(show ? k : 1f - k);
            yield return null;
        }
        ApplyScale(show ? 1f : 0f);

        if (!show)
            SetButtonsActive(false);
    }

    void ApplyScale(float factor)
    {
        if (muteObj != null) muteObj.transform.localScale = muteBaseScale * factor;
        if (quitObj != null) quitObj.transform.localScale = quitBaseScale * factor;
    }

    void SetButtonsActive(bool active)
    {
        if (muteObj != null) muteObj.SetActive(active);
        if (quitObj != null) quitObj.SetActive(active);
    }

    // --- Actions --------------------------------------------------------------

    void ToggleMute()
    {
        muted = !muted;
        AudioListener.volume = muted ? 0f : 1f;
        PlayerPrefs.SetInt(MuteKey, muted ? 1 : 0);
        UpdateMuteSprite();
    }

    void UpdateMuteSprite()
    {
        if (muteRenderer == null)
            return;
        Sprite target = muted ? mutedSprite : unmutedSprite;
        if (target != null)
            muteRenderer.sprite = target;
    }

    void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
