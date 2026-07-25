using System;
using UnityEngine;

public class WinScreen : MonoBehaviour
{
    public GameObject winScreenUI;
    public GameObject nextLevelButton;
    public GameObject menuButton;

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

    void Start()
    {
        // Hide the whole Win branch (sprite + canvas + buttons), not just this panel.
        BranchRoot.SetActive(false);
    }

    public void ShowWinScreen(int currentLevelIndex)
    {
        SnapToMainCamera();
        BranchRoot.SetActive(true);

        int totalLevels = UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings - 1;

        // Check if there is a next level
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
        // Load the next level if it exists
        int nextLevelIndex = currentLevelIndex + 1;
        if (nextLevelIndex <= PlayerPrefs.GetInt("LevelsUnlocked", 1))
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene($"Level {nextLevelIndex}");
        }
    }

    public void ReturnToMenu()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene("Menu");
    }
}