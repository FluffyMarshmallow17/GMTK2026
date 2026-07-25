using System;
using UnityEngine;

public class LoseScreen : MonoBehaviour
{
    public GameObject returnToMenuButton;
    public GameObject retryButton;

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

    void FitLoseArtToCamera()
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

        sr.transform.localScale = new Vector3(scale, scale, 1f);
        sr.transform.localPosition = new Vector3(0f, cam.orthographicSize * 0.2f, 0f);
        sr.sortingOrder = 500;
    }

    void Start()
    {
        // Hide the whole Lose branch (sprite + canvas + buttons), not just this panel.
        BranchRoot.SetActive(false);
    }

    public void ShowLoseScreen(int currentLevelIndex)
    {
        SnapToMainCamera();
        FitLoseArtToCamera();
        BranchRoot.SetActive(true);

        retryButton.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => RetryLevel(currentLevelIndex));
        returnToMenuButton.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => returnToMenu());
    }

    public void returnToMenu()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene("Menu");
    }

    public void RetryLevel(int currentLevelIndex)
    {
        Time.timeScale = 1f;
        // Reload the current level
        UnityEngine.SceneManagement.SceneManager.LoadScene($"Level {currentLevelIndex}");
    }
}