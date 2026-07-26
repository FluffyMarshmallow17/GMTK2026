using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// The mechanics a tutorial hint can teach, in the order the player meets them.
public enum TutorialStep
{
    PickUp,          // E — the first block comes into range
    CyclePickable,   // G — two or more blocks are in range at once
    CycleInventory,  // R — two or more blocks are held in inventory
    Shoot,           // F — shown right after CycleInventory
    Absorb,          // Q — shown right after CycleInventory
}

// Shows a one-time image hint the first time the player reaches a new mechanic.
// Gameplay code (Player.cs) just calls TutorialManager.Show(step); this class
// decides whether it's new, queues it, and plays hints one at a time so the
// player is never shown more than one thing to read at once.
public class TutorialManager : MonoBehaviour
{
    [Header("Tutorial images (Assets/images/tutorials)")]
    public Sprite ePickUp;          // first block enters range
    public Sprite gCyclePickable;   // 2+ blocks in range
    public Sprite rCycleInventory;  // 2+ blocks in inventory
    public Sprite fShoot;           // right after R
    public Sprite qAbsorb;          // right after R
    public Sprite symbolTutorial;   // auto-shown after all of the above

    [Header("Level intro (optional, plays on start — not triggered)")]
    [Tooltip("If set, this image plays once when the level begins, e.g. \"Introducing new symbols!\"")]
    public Sprite intro;

    [Header("Display")]
    [Tooltip("Height of the popup in pixels (width follows the image's aspect).")]
    public float popupHeight = 220f;
    [Tooltip("Gap from the bottom of the screen, in pixels.")]
    public float bottomMargin = 120f;
    [Tooltip("Seconds the hint stays fully visible.")]
    public float holdDuration = 3.5f;
    public float fadeDuration = 0.3f;

    // Which hints have already played — static so each hint shows once across the
    // whole play session (and every level), resetting only when the game restarts.
    static readonly HashSet<TutorialStep> shownThisSession = new HashSet<TutorialStep>();

    // The mechanic hints that must all play before the wrap-up symbol tutorial.
    static readonly TutorialStep[] mechanicHints =
    {
        TutorialStep.PickUp, TutorialStep.CyclePickable,
        TutorialStep.CycleInventory, TutorialStep.Shoot, TutorialStep.Absorb,
    };

    static TutorialManager instance;

    readonly Queue<Sprite> pending = new Queue<Sprite>();
    Image popup;
    bool playing;
    bool symbolQueued;

    void Awake()
    {
        instance = this;
        BuildPopup();
    }

    void Start()
    {
        // Level intro: not triggered by anything, just plays first when the level loads.
        Enqueue(intro);
    }

    void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    // Entry point for gameplay code. Safe to call even if no manager is in the scene.
    public static void Show(TutorialStep step)
    {
        if (instance != null)
            instance.ShowStep(step);
    }

    void ShowStep(TutorialStep step)
    {
        // Each mechanic hint plays at most once per session.
        if (shownThisSession.Contains(step))
            return;
        shownThisSession.Add(step);

        Enqueue(SpriteFor(step));

        // Once every mechanic hint has been reached, follow up with the symbol tutorial.
        if (!symbolQueued && AllMechanicHintsShown())
        {
            symbolQueued = true;
            Enqueue(symbolTutorial);
        }
    }

    // Queues an image to play. Ignores nulls (unassigned sprites) so callers stay simple.
    void Enqueue(Sprite image)
    {
        if (image == null)
            return;

        pending.Enqueue(image);
        if (!playing)
            StartCoroutine(PlayPending());
    }

    IEnumerator PlayPending()
    {
        playing = true;
        while (pending.Count > 0)
            yield return PlayHint(pending.Dequeue());
        playing = false;
    }

    bool AllMechanicHintsShown()
    {
        foreach (TutorialStep step in mechanicHints)
            if (!shownThisSession.Contains(step))
                return false;
        return true;
    }

    IEnumerator PlayHint(Sprite image)
    {
        if (popup == null || image == null)
            yield break;

        popup.sprite = image;
        popup.rectTransform.sizeDelta = new Vector2(popupHeight * Aspect(image), popupHeight);
        popup.enabled = true;

        yield return Fade(0f, 1f);
        yield return new WaitForSecondsRealtime(holdDuration);
        yield return Fade(1f, 0f);

        popup.enabled = false;
    }

    IEnumerator Fade(float from, float to)
    {
        for (float t = 0f; t < fadeDuration; t += Time.unscaledDeltaTime)
        {
            SetAlpha(Mathf.Lerp(from, to, t / fadeDuration));
            yield return null;
        }
        SetAlpha(to);
    }

    Sprite SpriteFor(TutorialStep step)
    {
        switch (step)
        {
            case TutorialStep.PickUp: return ePickUp;
            case TutorialStep.CyclePickable: return gCyclePickable;
            case TutorialStep.CycleInventory: return rCycleInventory;
            case TutorialStep.Shoot: return fShoot;
            case TutorialStep.Absorb: return qAbsorb;
            default: return null;
        }
    }

    static float Aspect(Sprite s) => s.rect.height > 0f ? s.rect.width / s.rect.height : 1f;

    void SetAlpha(float a)
    {
        Color c = popup.color;
        c.a = a;
        popup.color = c;
    }

    // Creates a screen-space overlay canvas with a single centered-bottom image,
    // so the only thing to wire up in the Inspector is the five sprites.
    void BuildPopup()
    {
        var canvasGO = new GameObject("TutorialCanvas", typeof(Canvas), typeof(CanvasScaler));
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000; // draw above the gameplay HUD

        var imageGO = new GameObject("TutorialPopup", typeof(Image));
        imageGO.transform.SetParent(canvasGO.transform, false);
        popup = imageGO.GetComponent<Image>();
        popup.raycastTarget = false;
        popup.preserveAspect = true;
        popup.enabled = false;

        RectTransform rt = popup.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f); // bottom-center
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, bottomMargin);

        SetAlpha(0f);
    }
}
