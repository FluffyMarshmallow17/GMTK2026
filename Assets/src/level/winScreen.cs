using System;
using UnityEngine;

public class WinScreen : MonoBehaviour
{
    public GameObject winScreenUI;
    public GameObject nextLevelButton;
    public GameObject menuButton;

    private void Start()
    {
        // Hide the win screen UI at the start of the game
        gameObject.SetActive(false);
    }

    public void ShowWinScreen(int currentLevelIndex)
    {
        // Show the win screen UI
        gameObject.SetActive(true);

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
        // Load the next level if it exists
        int nextLevelIndex = currentLevelIndex + 1;
        if (nextLevelIndex <= PlayerPrefs.GetInt("LevelsUnlocked", 1))
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene($"Level {nextLevelIndex}");
        }
    }

    public void ReturnToMenu()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("Menu");
    }
}