using System;
using UnityEngine;

public class LoseScreen : MonoBehaviour
{
    public GameObject loseScreenUI;
    public GameObject returnToMenuButton;
    public GameObject retryButton;

    private void Start()
    {
        // Hide the win screen UI at the start of the game
        gameObject.SetActive(false);
    }

    public void ShowLoseScreen(int currentLevelIndex)
    {
        // Show the lose screen UI
        gameObject.SetActive(true);

        retryButton.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => RetryLevel(currentLevelIndex));
        returnToMenuButton.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() => returnToMenu());
    }

    public void returnToMenu()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("Menu");
    }

    public void RetryLevel(int currentLevelIndex)
    {
        // Reload the current level
        UnityEngine.SceneManagement.SceneManager.LoadScene($"Level {currentLevelIndex}");
    }
}