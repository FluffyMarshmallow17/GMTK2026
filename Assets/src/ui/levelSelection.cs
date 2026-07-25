using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public Transform levelContainer;
    public LevelButton levelPrefab;
    public LevelButton[] levelButtons;

    [Tooltip("Fallback used only if no 'Level N' scenes are found in Build Settings.")]
    public int levelCount = 1;

    // TEMPORARY: unlocks every level button regardless of progress. Set to false to restore normal locking.
    public bool unlockAllLevels = true;

    void Start()
    {
        // Read the highest reached level from local storage (default to 1)
        int levelsUnlocked = PlayerPrefs.GetInt("LevelsUnlocked", 1);
        int totalLevels = DetermineLevelCount();
        levelButtons = new LevelButton[totalLevels];

        for (int level = 1; level <= totalLevels; level++)
        {
            LevelButton button =
                Instantiate(levelPrefab, levelContainer);

            levelButtons[level - 1] = button; // Store the button reference
            button.SetText($"{level}");
            button.gameObject.SetActive(true); // Always show every level button

            int enterLevel = level;
            button.Button.onClick.AddListener(() =>
            {
                loadLevel(enterLevel);
            });

            // Only gate interaction (locked levels still show, just aren't clickable)
            button.Button.interactable = unlockAllLevels || enterLevel <= levelsUnlocked;
        }

        levelPrefab.gameObject.SetActive(false); // Hide the prefab
    }

    // Scans Build Settings for scenes named "Level {n}" and returns the highest n found,
    // so newly added level scenes automatically show up without editing an Inspector value.
    int DetermineLevelCount()
    {
        int highest = 0;
        int sceneCount = SceneManager.sceneCountInBuildSettings;
        for (int i = 0; i < sceneCount; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            string name = Path.GetFileNameWithoutExtension(path);
            const string prefix = "Level ";
            if (name.StartsWith(prefix) && int.TryParse(name.Substring(prefix.Length), out int levelNumber))
            {
                highest = Mathf.Max(highest, levelNumber);
            }
        }
        return highest > 0 ? highest : levelCount;
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
