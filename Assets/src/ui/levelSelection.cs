using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// World-space level select: floating glowing number sprites, a Select marker
// that locks onto the highlighted level, and left/right + Enter (or a click)
// to play it.
public class SceneLoader : MonoBehaviour
{
    [Header("Level icons")]
    [Tooltip("Number sprites in order: element 0 = level 1, element 1 = level 2, ...")]
    public Sprite[] numberSprites;
    [Tooltip("Horizontal gap between level numbers (world units).")]
    public float spacing = 3f;
    [Tooltip("Levels per row.")]
    public int columns = 4;
    [Tooltip("Vertical gap between rows (world units).")]
    public float rowSpacing = 3f;
    [Tooltip("World-space scale applied to each spawned number sprite.")]
    public float iconScale = 1f;
    [Tooltip("Sorting order for the numbers (the marker draws behind them).")]
    public int sortingOrder = 10;
    [Tooltip("Optional glow material for the numbers. Leave empty for the default sprite material.")]
    public Material iconMaterial;

    [Header("Selection marker")]
    [Tooltip("The Select prefab that highlights the currently chosen level.")]
    public GameObject selectPrefab;
    [Tooltip("How quickly the marker slides onto the selected level.")]
    public float markerFollowSmoothTime = 0.08f;

    [Header("Levels")]
    [Tooltip("Fallback used only if no 'Level N' scenes are found in Build Settings.")]
    public int levelCount = 1;

    // TEMPORARY: unlocks every level regardless of progress. Set to false to restore normal locking.
    public bool unlockAllLevels = false; // TODO

    readonly List<LevelIcon> icons = new List<LevelIcon>();
    Transform marker;
    Vector3 markerVelocity;
    int selectedIndex = -1;

    void Start()
    {
        AudioManager.Instance.PlayMenuMusic();
        int levelsUnlocked = PlayerPrefs.GetInt("LevelsUnlocked", 1);
        int totalLevels = DetermineLevelCount();

        int cols = Mathf.Max(1, columns);
        int rows = Mathf.CeilToInt(totalLevels / (float)cols);

        for (int level = 1; level <= totalLevels; level++)
        {
            int i = level - 1;
            int row = i / cols;
            int col = i % cols;
            int itemsInRow = Mathf.Min(cols, totalLevels - row * cols);

            // Center each row horizontally, and the whole block vertically (row 0 on top).
            float x = (col - (itemsInRow - 1) * 0.5f) * spacing;
            float y = ((rows - 1) * 0.5f - row) * rowSpacing;

            bool unlocked = unlockAllLevels || level <= levelsUnlocked;
            icons.Add(CreateIcon(level, new Vector3(x, y, 0f), unlocked));
        }

        if (selectPrefab != null)
            marker = Instantiate(selectPrefab, transform).transform;

        // Start on the first playable level.
        selectedIndex = icons.FindIndex(i => i.selectable);
        if (selectedIndex < 0) selectedIndex = 0;
        if (marker != null && icons.Count > 0)
            marker.position = icons[selectedIndex].transform.position;

        UpdateSelectionVisuals();
    }

    LevelIcon CreateIcon(int level, Vector3 localPos, bool unlocked)
    {
        var go = new GameObject($"Level {level}");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = Vector3.one * iconScale;

        var sr = go.AddComponent<SpriteRenderer>();
        int spriteIndex = level - 1;
        if (numberSprites != null && spriteIndex >= 0 && spriteIndex < numberSprites.Length)
            sr.sprite = numberSprites[spriteIndex];
        sr.sortingOrder = sortingOrder;
        if (iconMaterial != null) sr.sharedMaterial = iconMaterial;

        var col = go.AddComponent<BoxCollider2D>();
        if (sr.sprite != null) col.size = sr.sprite.bounds.size;

        var icon = go.AddComponent<LevelIcon>();
        icon.Init(level, unlocked);
        return icon;
    }

    void Update()
    {
        HandleKeyboard();
        HandleMouse();
        FollowMarker();
    }

    void HandleKeyboard()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame)
            Move(-1);
        else if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame)
            Move(1);

        if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
            ConfirmSelection();
    }

    void HandleMouse()
    {
        var mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 world = cam.ScreenToWorldPoint(mouse.position.ReadValue());
        Collider2D hit = Physics2D.OverlapPoint(world);
        if (hit == null) return;

        var icon = hit.GetComponent<LevelIcon>();
        if (icon == null || !icon.selectable) return;

        int idx = icons.IndexOf(icon);
        if (idx < 0) return;

        // Clicking a level locks the marker onto it and plays it.
        if (idx != selectedIndex)
            PlaySelectSound();
        selectedIndex = idx;
        UpdateSelectionVisuals();
        ConfirmSelection();
    }

    // Steps the selection in `dir`, skipping locked levels and stopping at the ends.
    void Move(int dir)
    {
        int next = selectedIndex;
        for (int step = 0; step < icons.Count; step++)
        {
            next += dir;
            if (next < 0 || next >= icons.Count) return;
            if (icons[next].selectable)
            {
                selectedIndex = next;
                UpdateSelectionVisuals();
                PlaySelectSound();
                return;
            }
        }
    }

    void PlaySelectSound()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(SFX.Target);
    }

    void ConfirmSelection()
    {
        if (selectedIndex < 0 || selectedIndex >= icons.Count) return;
        LevelIcon icon = icons[selectedIndex];
        if (icon.selectable)
            loadLevel(icon.levelNumber);
    }

    void UpdateSelectionVisuals()
    {
        for (int i = 0; i < icons.Count; i++)
            icons[i].SetSelected(i == selectedIndex);
    }

    void FollowMarker()
    {
        if (marker == null || selectedIndex < 0 || selectedIndex >= icons.Count) return;
        marker.position = Vector3.SmoothDamp(
            marker.position,
            icons[selectedIndex].transform.position,
            ref markerVelocity,
            markerFollowSmoothTime);
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

    // Load a scene by its "Level {n}" name.
    public void loadLevel(int level)
    {
        loadLevelByName($"Level {level}");
    }

    // Load by scene name.
    public void loadLevelByName(string levelName)
    {
        SceneManager.LoadScene(levelName);
    }

    // Call this from your level's "Finish Line" script to unlock the next level.
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
