using System;
using System.Collections;
using UnityEngine;

public class ScreenFade : MonoBehaviour
{
    public enum Ease
    {
        Linear,
        SmoothStep,
        EaseInCubic,
        EaseOutCubic,
        EaseInOutCubic,
        EaseInExpo,
        EaseOutExpo,
    }

    [SerializeField] int sortingOrder = 400;

    SpriteRenderer overlay;
    Coroutine fadeRoutine;
    static Sprite whiteSprite;

    void Awake()
    {
        EnsureOverlay();
        SetColor(Color.black, 0f);
    }

    void LateUpdate()
    {
        FitOverlayToCamera(Camera.main);
    }

    public void FadeToBlack(Action onComplete)
    {
        EnsureOverlay();
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeToColorRoutine(Color.black, 0.6f, Ease.EaseOutCubic, onComplete));
    }

    /// <summary>Flash to white, hold, then settle to black. Uses unscaled time.</summary>
    public void PlayWhiteToBlack(
        float toWhiteDuration,
        float holdWhite,
        float toBlackDuration,
        Action onComplete,
        Ease toWhiteEase = Ease.EaseInExpo,
        Ease toBlackEase = Ease.EaseOutCubic)
    {
        EnsureOverlay();
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(WhiteToBlackRoutine(
            toWhiteDuration, holdWhite, toBlackDuration, toWhiteEase, toBlackEase, onComplete));
    }

    public void FadeToWhite(float duration, Ease ease, Action onComplete)
    {
        EnsureOverlay();
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeToColorRoutine(Color.white, duration, ease, onComplete));
    }

    public void FadeToBlack(float duration, Ease ease, Action onComplete)
    {
        EnsureOverlay();
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeToColorRoutine(Color.black, duration, ease, onComplete));
    }

    IEnumerator WhiteToBlackRoutine(
        float toWhite, float hold, float toBlack, Ease toWhiteEase, Ease toBlackEase, Action onComplete)
    {
        yield return FadeToColorRoutine(Color.white, toWhite, toWhiteEase, null);
        if (hold > 0f)
            yield return new WaitForSecondsRealtime(hold);
        yield return FadeToColorRoutine(Color.black, toBlack, toBlackEase, null);
        onComplete?.Invoke();
    }

    IEnumerator FadeToColorRoutine(Color target, float duration, Ease ease, Action onComplete)
    {
        EnsureOverlay();
        Color start = overlay.color;
        Color end = target;
        end.a = 1f;

        if (duration <= 0f)
        {
            overlay.color = end;
            onComplete?.Invoke();
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float u = Evaluate(ease, Mathf.Clamp01(elapsed / duration));
            overlay.color = Color.LerpUnclamped(start, end, u);
            yield return null;
        }

        overlay.color = end;
        onComplete?.Invoke();
    }

    public static float Evaluate(Ease ease, float t)
    {
        t = Mathf.Clamp01(t);
        switch (ease)
        {
            case Ease.EaseInCubic: return t * t * t;
            case Ease.EaseOutCubic: return 1f - Mathf.Pow(1f - t, 3f);
            case Ease.EaseInOutCubic:
                return t < 0.5f
                    ? 4f * t * t * t
                    : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;
            case Ease.EaseInExpo: return t <= 0f ? 0f : Mathf.Pow(2f, 10f * t - 10f);
            case Ease.EaseOutExpo: return t >= 1f ? 1f : 1f - Mathf.Pow(2f, -10f * t);
            case Ease.SmoothStep: return t * t * (3f - 2f * t);
            default: return t;
        }
    }

    void EnsureOverlay()
    {
        if (overlay != null)
            return;

        Camera cam = Camera.main;
        if (cam == null)
            return;

        var go = new GameObject("ScreenFadeOverlay");
        go.transform.SetParent(cam.transform, false);
        go.transform.localPosition = new Vector3(0f, 0f, 1f);

        overlay = go.AddComponent<SpriteRenderer>();
        overlay.sprite = GetWhiteSprite();
        overlay.sortingOrder = sortingOrder;
        SetColor(Color.black, 0f);
    }

    void FitOverlayToCamera(Camera cam)
    {
        if (overlay == null || cam == null || !cam.orthographic)
            return;

        float height = cam.orthographicSize * 2f;
        float width = height * cam.aspect;
        // Slight overscan so shake never reveals edges.
        overlay.transform.localScale = new Vector3(width * 1.15f, height * 1.15f, 1f);
    }

    void SetColor(Color color, float alpha)
    {
        if (overlay == null)
            return;
        color.a = alpha;
        overlay.color = color;
    }

    static Sprite GetWhiteSprite()
    {
        if (whiteSprite != null)
            return whiteSprite;

        var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        whiteSprite = Sprite.Create(tex, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        return whiteSprite;
    }
}
