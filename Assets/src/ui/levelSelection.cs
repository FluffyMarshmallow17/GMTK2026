using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public Transform levelContainer;
    public LevelButton levelPrefab;
    public LevelButton[] levelButtons;
    public int levelCount = 1; // Total number of levels in the game    

    void Start()
    {
        // Read the highest reached level from local storage (default to 1)
        int levelsUnlocked = PlayerPrefs.GetInt("LevelsUnlocked", 1);
        levelButtons = new LevelButton[levelCount];

        for (int level = 1; level <= levelCount; level++)
        {
            Debug.Log($"Level {level}");
            LevelButton button =
                Instantiate(levelPrefab, levelContainer);

            levelButtons[level - 1] = button; // Store the button reference
            button.SetText($"{level}");

            int enterLevel = level;
            button.Button.onClick.AddListener(() =>
            {
                loadLevel(enterLevel);
            });
        }

        levelPrefab.gameObject.SetActive(false); // Hide the prefab

        for (int i = 0; i < levelButtons.Length; i++)
        {
            // If the loop index is higher than progress, disable interaction
            if (i + 1 > levelsUnlocked)
            {
                levelButtons[i].Button.interactable = false;
            }
        }
    }

    // Load a scene by its Build Settings index
    public void loadLevel(int level)
    {
        loadLevelByName($"Level {level}");
    }

    // Load by scene name
    public void loadLevelByName(string levelName)
    {
        SceneManager.LoadScene(levelName);
    }

    // Call this function from your level's "Finish Line" script to unlock the next level
    public static void UnlockNextLevel(int currentLevelIndex)
    {
        int highestUnlocked = PlayerPrefs.GetInt("LevelsUnlocked", 1);

        if (currentLevelIndex >= highestUnlocked)
        {
            PlayerPrefs.SetInt("LevelsUnlocked", currentLevelIndex + 1);
            PlayerPrefs.Save();
        }
    }
}