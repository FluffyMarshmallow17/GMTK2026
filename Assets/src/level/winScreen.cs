using System.Collections;
using UnityEngine;

public class WinScreen : MonoBehaviour
{
    static readonly int GlowAmountId = Shader.PropertyToID("_GlowAmount");

    const float RevealDuration = 0.6f;
    const float BaseGlow = 1f;
    const float PeakGlow = 3.75f;
    const float PeakScaleMultiplier = 1.12f;

    public GameObject winScreenUI;
    public GameObject nextLevelButton;
    public GameObject menuButton;

    SpriteRenderer winArt;
    Vector3 winArtBaseScale;
    Material winArtMaterial;
    Coroutine revealRoutine;
    bool transitioning;
    public AudioClip WinMusic;

    GameObject BranchRoot =>
        transform.parent != null && transform.parent.parent != null
            ? transform.parent.parent.gameObject
            : gameObject;

    void SnapToMainCamera()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Transform root = BranchRoot.transform.root;
        Vector3 p = cam.transform.position;
        root.position = new Vector3(p.x, p.y, 0f);
    }

    void RevealAmbientBackground()
    {
        Transform root = BranchRoot.transform.root;
        AmbientGridPulses.ActivateUnder(root);
        ScreenFade fade = root.GetComponentInChildren<ScreenFade>(true);
        if (fade == null)
            fade = FindAnyObjectByType<ScreenFade>();
        if (fade != null)
            fade.FadeFromWhite(0.9f, ScreenFade.Ease.EaseOutCubic, null);
        else
            fade?.ClearOverlay();
    }

    void FitWinArtToCamera()
    {
        SpriteRenderer sr = BranchRoot.GetComponentInChildren<SpriteRenderer>(true);
        if (sr == null || sr.sprite == null)
            return;

        Camera cam = Camera.main;
        if (cam == null || !cam.orthographic)
            return;

        float viewHeight = cam.orthographicSize * 2f;
        float viewWidth = viewHeight * cam.aspect;
        Vector2 spriteSize = sr.sprite.bounds.size;

        const float widthPadding = 0.8f;
        const float heightFraction = 0.35f;
        float scale = Mathf.Min(
            viewWidth * widthPadding / spriteSize.x,
            viewHeight * heightFraction / spriteSize.y);

        winArt = sr;
        winArtBaseScale = new Vector3(scale, scale, 1f);
        sr.transform.localScale = winArtBaseScale;
        sr.transform.localPosition = new Vector3(0f, cam.orthographicSize * 0.2f, 0f);
        sr.sortingOrder = 500;

        winArtMaterial = sr.material;
        if (winArtMaterial != null && winArtMaterial.HasProperty(GlowAmountId))
            winArtMaterial.SetFloat(GlowAmountId, BaseGlow);
    }

    void PlayWinReveal()
    {
        if (winArt == null)
            return;

        if (revealRoutine != null)
            StopCoroutine(revealRoutine);

        revealRoutine = StartCoroutine(WinRevealRoutine());
    }

    IEnumerator WinRevealRoutine()
    {
        float elapsed = 0f;
        while (elapsed < RevealDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float pulse = Mathf.Sin(Mathf.Clamp01(elapsed / RevealDuration) * Mathf.PI);
            float glow = Mathf.Lerp(BaseGlow, PeakGlow, pulse);
            float scaleMul = Mathf.Lerp(1f, PeakScaleMultiplier, pulse);

            if (winArtMaterial != null && winArtMaterial.HasProperty(GlowAmountId))
                winArtMaterial.SetFloat(GlowAmountId, glow);

            winArt.transform.localScale = winArtBaseScale * scaleMul;
            yield return null;
        }

        if (winArtMaterial != null && winArtMaterial.HasProperty(GlowAmountId))
            winArtMaterial.SetFloat(GlowAmountId, BaseGlow);
        winArt.transform.localScale = winArtBaseScale;
        revealRoutine = null;
    }

    void Start()
    {
        BranchRoot.SetActive(false);
    }

    public void ShowWinScreen(int currentLevelIndex)
    {
        
        CameraShake.Clear();
        SnapToMainCamera();
        RevealAmbientBackground();
        FitWinArtToCamera();
        BranchRoot.SetActive(true);
        PlayWinReveal();
        AudioManager.Instance.PlayMusic(WinMusic); 

        int totalLevels = UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings - 1;

        if (currentLevelIndex < totalLevels)
        {
            nextLevelButton.SetActive(true);
            nextLevelButton.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => LoadNextLevel(currentLevelIndex));
        }
        else
        {
            nextLevelButton.SetActive(false);
        }

        menuButton.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => ReturnToMenu());
    }

    public void LoadNextLevel(int currentLevelIndex)
    {
        Time.timeScale = 1f;
        int nextLevelIndex = currentLevelIndex + 1;
        if (nextLevelIndex <= PlayerPrefs.GetInt("LevelsUnlocked", 1))
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene($"Level {nextLevelIndex}");
        }
    }

    public void ReturnToMenu()
    {
        if (transitioning)
            return;

        transitioning = true;
        if (menuButton != null)
            menuButton.GetComponent<UnityEngine.UI.Button>().interactable = false;
        if (nextLevelButton != null)
            nextLevelButton.GetComponent<UnityEngine.UI.Button>().interactable = false;

        ScreenFade.TransitionToMenu(this);
    }
}
