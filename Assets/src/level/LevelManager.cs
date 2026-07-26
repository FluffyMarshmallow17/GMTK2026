using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// Runs before Player/Boss Awake so it can set their countdowns from LevelData
// before their displays initialize.
[DefaultExecutionOrder(-100)]
public class LevelManager : MonoBehaviour
{
    float time;
    float playerTime;
    float bossTime;
    float miniEnemyTime;
    public Player player;
    public Boss boss;
    public List<MiniEnemy> miniEnemies;
    public GameObject miniEnemyPrefab;
    public GameObject map;
    public GameObject winScreen;
    public GameObject loseScreen;
    public bool levelEnded;

    public GameObject blockPrefab;
    public GameObject zonePrefab;
    public LevelData levelData;
    public AudioClip[] musicTracks;

    float zoneTime;

    const float SlowMoDiveDuration = 0.95f;
    const float SlowMoScale = 0.12f;
    const float CameraLockDuration = 1.05f;
    // Drawn-out final tick: hold on the last digit, then drop to 0.
    const float LingerOnFinalTick = 1.6f;
    const float PunchDuration = 1.05f;
    const float PreShakeDeepen = 0.75f;
    const float DeepSlowMoScale = 0.06f;
    const float ShakeRampDuration = 2.1f;
    const float ShakeStartTrauma = 0.22f;
    const float ShakePeakTrauma = 0.9f;
    const float ShakeMaxOffset = 1.9f;
    const float FadeToWhite = 1.1f;
    const float HoldWhite = 0.25f;
    const float FadeFromWhite = 0.9f;

    [Tooltip("How far directly below the boss the player spawns (world units).")]
    public float playerSpawnBelowBoss = 15f;

    int numberSpriteCount;

    void Awake()
    {
        Time.timeScale = 1f;
        levelEnded = false;
        time = 0;
        playerTime = 0;
        bossTime = 0;
        miniEnemies = new List<MiniEnemy>();
        zoneTime = 0f;
        miniEnemyTime = 0f;
        numberSpriteCount = blockPrefab.GetComponent<Block>().NumberSpriteCount;
        player.setCountdown(levelData.initialPlayerCount);
        boss.setCountdown(levelData.initialBossCount);
    }

    void Start()
    {
        // Spawn the player directly below the boss (same X, offset down in Y).
        if (player != null && boss != null)
        {
            Vector3 spawn = boss.transform.position;
            spawn.y -= playerSpawnBelowBoss;
            player.transform.position = spawn;

            Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
            if (playerRb != null)
                playerRb.position = spawn; // keep physics in sync with the teleport
        }

        Map mapScript = map.GetComponent<Map>();
        if (levelData.changingBorders)
            mapScript.snapToCountdown(GetTotalCountdown());
        else
            mapScript.snapToRadius(levelData.hardcodeRadius);

        AudioManager.Instance.PlayMusic(GetRandomTrack());
    }

    AudioClip GetRandomTrack()
{
    if (musicTracks == null || musicTracks.Length == 0)
        return null;
    return musicTracks[UnityEngine.Random.Range(0, musicTracks.Length)];
}

    void FixedUpdate()
    {
        if (levelEnded)
            return;

        time += Time.deltaTime;
        playerTime += Time.deltaTime;
        bossTime += Time.deltaTime;
        if (playerTime >= (1 * player.getRate()))
        {
            playerTime = 0;
            player.decreaseCountdown();
            if (player.getCountdown() <= 0)
            {
                int currentLevelIndex = GetCurrentLevelIndex();
                EndLevel(
                    player.transform,
                    player.display,
                    () => loseScreen.GetComponent<LoseScreen>().ShowLoseScreen(currentLevelIndex));
            }
        }
        if (bossTime >= (1 * boss.getRate()))
        {
            bossTime = 0;
            boss.decreaseCountdown();
            if (boss.getCountdown() <= 0)
            {
                int currentLevelIndex = GetCurrentLevelIndex();
                PlayerPrefs.SetInt("LevelsUnlocked", Math.Max(PlayerPrefs.GetInt("LevelsUnlocked", 1), currentLevelIndex + 1));
                EndLevel(
                    boss.transform,
                    boss.display,
                    () => winScreen.GetComponent<WinScreen>().ShowWinScreen(currentLevelIndex));
            }
        }
        float spawnInterval = GetBlockSpawnInterval();
        if (time >= spawnInterval)
        {
            time = 0;
            spawnBlock();
        }
        if (levelData.spawnZones && zonePrefab != null)
        {
            zoneTime += Time.deltaTime;
            if (zoneTime >= Mathf.Max(0.01f, levelData.zoneSpawnRate))
            {
                zoneTime = 0f;
                TrySpawnZone();
            }
        }
        if (levelData.includeMiniEnemies)
        {
            miniEnemyTime += Time.deltaTime;
            if (miniEnemyTime >= Mathf.Max(0.01f, levelData.miniEnemySpawnRate))
            {
                miniEnemyTime = 0f;
                addMiniEnemy(UnityEngine.Random.Range(5, 15));
            }
        }
        for (int i = miniEnemies.Count - 1; i >= 0; i--)
        {
            MiniEnemy mini = miniEnemies[i];
            if (mini == null)
            {
                miniEnemies.RemoveAt(i);
                continue;
            }

            mini.time += Time.deltaTime;
            if (mini.time >= (1 * mini.getRate()))
            {
                mini.time = 0;
                mini.decreaseCountdown();
            }
        }

        if (levelData.changingBorders)
            map.GetComponent<Map>().resizeMap(GetTotalCountdown());
    }

    void EndLevel(Transform focus, TextMeshPro countdownLabel, Action showScreen)
    {
        if (levelEnded)
            return;
        levelEnded = true;
        StartCoroutine(LevelEndCinematic(focus, countdownLabel, showScreen));
    }

    IEnumerator LevelEndCinematic(Transform focus, TextMeshPro countdownLabel, Action showScreen)
    {
        AudioManager.Instance.PlaySFX(SFX.Boom);
        if (player != null)
        {
            Rigidbody2D playerRb = player.GetComponent<Rigidbody2D>();
            player.BeginCoastMovement(playerRb != null ? playerRb.linearVelocity : Vector2.zero);
        }

        Camera cam = Camera.main;
        CameraFollow follow = cam != null ? cam.GetComponent<CameraFollow>() : null;
        // Whip-pan onto the dying clock while timeScale eases into slow-mo.
        follow?.LockOntoEased(focus, CameraLockDuration);
        yield return AnimateTimeScale(Time.timeScale, SlowMoScale, SlowMoDiveDuration, ScreenFade.Ease.EaseOutExpo);

        // Hold the last non-zero digit so the tick to 0 can be drawn out.
        if (focus == player.transform)
            player.FreezeDisplay(1);
        else if (focus == boss.transform)
            boss.FreezeDisplay(1);
        else if (countdownLabel != null)
            countdownLabel.text = "1";

        // Soft rumble builds during the linger (still before the zero).
        CameraShake.BeginCinematic(ShakeMaxOffset);
        float lingerElapsed = 0f;
        while (lingerElapsed < LingerOnFinalTick)
        {
            lingerElapsed += Time.unscaledDeltaTime;
            float u = ScreenFade.Evaluate(ScreenFade.Ease.EaseInCubic, Mathf.Clamp01(lingerElapsed / LingerOnFinalTick));
            CameraShake.SetTrauma(Mathf.Lerp(0.08f, ShakeStartTrauma, u));
            yield return null;
        }

        // Zero-tick: punch to 0, intensify shake, fade to white — all overlapping.
        CameraShake.SetTrauma(ShakeStartTrauma);

        bool whiteDone = false;
        ScreenFade fade = GetScreenFade();
        if (fade != null)
            fade.FadeToWhite(FadeToWhite, ScreenFade.Ease.EaseInCubic, () => whiteDone = true);
        else
            whiteDone = true;

        if (countdownLabel != null)
        {
            if (focus == player.transform)
            {
                player.FreezeDisplay(0);
            }
            else if (focus == boss.transform)
            {
                boss.FreezeDisplay(0);
            }
            else
            {
                countdownLabel.text = "0";
            }
            StartCoroutine(PunchLabel(countdownLabel, PunchDuration));
        }

        // Ramp shake small → larger while white builds; also ease deeper into slow-mo.
        float rampElapsed = 0f;
        float timeFrom = Time.timeScale;
        while (rampElapsed < ShakeRampDuration)
        {
            rampElapsed += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(rampElapsed / ShakeRampDuration);
            float shakeU = ScreenFade.Evaluate(ScreenFade.Ease.EaseInCubic, u);
            CameraShake.SetTrauma(Mathf.Lerp(ShakeStartTrauma, ShakePeakTrauma, shakeU));

            float deepU = ScreenFade.Evaluate(ScreenFade.Ease.EaseInCubic, Mathf.Clamp01(rampElapsed / PreShakeDeepen));
            Time.timeScale = Mathf.Lerp(timeFrom, DeepSlowMoScale, deepU);
            yield return null;
        }
        CameraShake.SetTrauma(ShakePeakTrauma);
        Time.timeScale = DeepSlowMoScale;

        while (!whiteDone)
            yield return null;

        if (HoldWhite > 0f)
            yield return new WaitForSecondsRealtime(HoldWhite);

        CameraShake.ReleaseCinematic(0.55f);

        Time.timeScale = 0f;
        HideLevelGameplay();
        showScreen?.Invoke();
    }

    void HideLevelGameplay()
    {
        if (player != null)
            player.gameObject.SetActive(false);
        if (boss != null)
            boss.gameObject.SetActive(false);
        if (map != null)
            map.SetActive(false);

        foreach (Block block in FindObjectsByType<Block>(FindObjectsSortMode.None))
            block.gameObject.SetActive(false);

        if (miniEnemies != null)
        {
            for (int i = miniEnemies.Count - 1; i >= 0; i--)
            {
                if (miniEnemies[i] != null)
                    miniEnemies[i].gameObject.SetActive(false);
            }
        }

        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
        }
    }

    IEnumerator AnimateTimeScale(float from, float to, float duration, ScreenFade.Ease ease)
    {
        if (duration <= 0f)
        {
            Time.timeScale = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float u = ScreenFade.Evaluate(ease, Mathf.Clamp01(elapsed / duration));
            Time.timeScale = Mathf.LerpUnclamped(from, to, u);
            yield return null;
        }
        Time.timeScale = to;
    }

    IEnumerator PunchLabel(TextMeshPro label, float duration)
    {
        if (label == null)
            yield break;

        Transform t = label.transform;
        Vector3 baseScale = t.localScale;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(elapsed / duration);
            // Fast swell (ease-out), then a longer ease-back settle.
            float swell = u < 0.35f
                ? ScreenFade.Evaluate(ScreenFade.Ease.EaseOutExpo, u / 0.35f)
                : 1f - ScreenFade.Evaluate(ScreenFade.Ease.EaseInOutCubic, (u - 0.35f) / 0.65f);
            float s = 1f + 0.7f * Mathf.Max(0f, swell);
            t.localScale = baseScale * s;
            yield return null;
        }
        t.localScale = baseScale;
    }

    int GetCurrentLevelIndex()
    {
        string currentLevel = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Remove(0, 6);
        return int.Parse(currentLevel);
    }

    ScreenFade GetScreenFade()
    {
        Transform root = loseScreen != null ? loseScreen.transform.root : transform;
        ScreenFade fade = root.GetComponent<ScreenFade>();
        if (fade == null)
            fade = root.gameObject.AddComponent<ScreenFade>();
        return fade;
    }

    int GetTotalCountdown()
    {
        int totalCountdown = player.getCountdown() + boss.getCountdown();
        if (miniEnemies != null)
        {
            for (int i = miniEnemies.Count - 1; i >= 0; i--)
            {
                MiniEnemy enemy = miniEnemies[i];
                if (enemy == null)
                {
                    miniEnemies.RemoveAt(i);
                    continue;
                }
                totalCountdown += enemy.getCountdown();
            }
        }
        return totalCountdown;
    }

    float GetBlockSpawnInterval()
    {
        // Interval grows as the field fills: blockRate / (1 - count/maxBlocks).
        // e.g. maxBlocks=50, count=48 → multiplier 25 → very slow spawns.
        int maxBlocks = GetEffectiveMaxBlocks();
        if (maxBlocks <= 0)
            return Mathf.Max(0.01f, levelData.blockRate);

        int count = CountBlocks();
        if (count >= maxBlocks)
            return float.PositiveInfinity;

        float fill = (float)count / maxBlocks;
        return Mathf.Max(0.01f, levelData.blockRate / (1f - fill));
    }

    static int CountBlocks()
    {
        return FindObjectsByType<Block>(FindObjectsSortMode.None).Length;
    }

    /// <summary>
    /// Spawn outer radius: at least levelData.spawnMax, or the map border if larger.
    /// </summary>
    float GetEffectiveSpawnMax()
    {
        float spawnMax = levelData.spawnMax;
        if (map == null)
            return spawnMax;

        Map mapScript = map.GetComponent<Map>();
        if (mapScript == null)
            return spawnMax;

        float mapRadius = mapScript.GetRadius();
        if (spawnMax < mapRadius)
            return mapRadius;
        return spawnMax;
    }

    /// <summary>
    /// maxBlocks scaled by spawn area when the map is larger than spawnMax,
    /// so block density stays roughly constant as the border grows.
    /// </summary>
    int GetEffectiveMaxBlocks()
    {
        if (levelData.maxBlocks <= 0)
            return levelData.maxBlocks;

        float configuredMax = Mathf.Max(0.01f, levelData.spawnMax);
        float effectiveMax = GetEffectiveSpawnMax();
        if (effectiveMax <= configuredMax)
            return levelData.maxBlocks;

        // Area scales with r^2.
        float areaScale = (effectiveMax * effectiveMax) / (configuredMax * configuredMax);
        return Mathf.Max(1, Mathf.RoundToInt(levelData.maxBlocks * areaScale));
    }

    public void spawnBlock()
    {
        int maxBlocks = GetEffectiveMaxBlocks();
        if (maxBlocks > 0 && CountBlocks() >= maxBlocks)
            return;

        float spawnMax = GetEffectiveSpawnMax();
        float minRadius = Mathf.Min(levelData.spawnMin, spawnMax);
        float maxRadius = Mathf.Max(levelData.spawnMin, spawnMax);
        float radius = UnityEngine.Random.Range(minRadius, maxRadius);
        float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        Vector3 spawnPosition = new Vector3(
            Mathf.Cos(angle) * radius,
            Mathf.Sin(angle) * radius,
            0
        );

        Block block = Instantiate(blockPrefab, spawnPosition, Quaternion.identity)
            .GetComponent<Block>();

        RollBlockType(out bool spawnNumber, out int typeIndex);
        if (spawnNumber)
            block.SetNumber(typeIndex);
        else
            block.SetOperation((operationType)typeIndex);
    }

    // Rolls a block type from the level's probabilities. When isNumber is true,
    // index is a number-sprite index; otherwise it's an operationType value.
    // Shared by regular block spawning and by zones (so a zone's type follows the
    // same odds as the loose blocks around it).
    public void RollBlockType(out bool isNumber, out int index)
    {
        float categoryTotal = levelData.numberProbability + levelData.operationProbability;
        isNumber = categoryTotal <= 0f
            || UnityEngine.Random.value < levelData.numberProbability / categoryTotal;

        if (isNumber)
            index = PickWeightedIndex(levelData.numberProbabilities, numberSpriteCount);
        else
            index = PickWeightedIndex(
                new[] { levelData.addProbability, levelData.subtractProbability, levelData.multiplyProbability, levelData.divideProbability, levelData.decayProbability, levelData.growProbability },
                6);
    }

    // Spawns one zone at a spot that doesn't overlap existing zones. If no clear
    // spot is found after a few tries, it skips this attempt (tries again later).
    void TrySpawnZone()
    {
        int size = UnityEngine.Random.Range(levelData.zoneSizeMin, levelData.zoneSizeMax + 1);
        if (!TryFindZonePosition(size, out Vector3 position))
            return;

        int cost = UnityEngine.Random.Range(levelData.zoneCostMin, levelData.zoneCostMax + 1);
        int blockCount = UnityEngine.Random.Range(levelData.zoneBlocksMin, levelData.zoneBlocksMax + 1);
        RollBlockType(out bool isNumber, out int typeIndex);

        Zone zone = Instantiate(zonePrefab, position, Quaternion.identity).GetComponent<Zone>();
        zone.Initialize(size, cost, levelData.zoneLifespan, isNumber, typeIndex, blockCount, blockPrefab);
    }

    bool TryFindZonePosition(float size, out Vector3 position)
    {
        float outer = GetEffectiveSpawnMax();
        float inner = Mathf.Min(levelData.spawnMin, outer);
        Zone[] existing = FindObjectsByType<Zone>(FindObjectsSortMode.None);

        // Keep the whole zone clear of the player so it never spawns on top of or
        // reaches them as it grows (radius + a little breathing room).
        bool hasPlayer = player != null;
        Vector3 playerPos = hasPlayer ? player.transform.position : Vector3.zero;
        float playerClearance = size + 4f;

        for (int attempt = 0; attempt < 30; attempt++)
        {
            float r = UnityEngine.Random.Range(inner, outer);
            float a = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            Vector3 candidate = new Vector3(Mathf.Cos(a) * r, Mathf.Sin(a) * r, 0f);

            if (hasPlayer && (candidate - playerPos).sqrMagnitude < playerClearance * playerClearance)
                continue;
            if (OverlapsAnyZone(candidate, size, existing))
                continue;

            position = candidate;
            return true;
        }
        position = Vector3.zero;
        return false;
    }

    // A candidate overlaps an existing zone if the gap between their centers is
    // less than the sum of their radii (plus a little breathing room).
    static bool OverlapsAnyZone(Vector3 position, float size, Zone[] zones)
    {
        const float padding = 1f;
        foreach (Zone zone in zones)
        {
            if (zone == null) continue;
            float minDistance = zone.size + size + padding;
            if ((zone.transform.position - position).sqrMagnitude < minDistance * minDistance)
                return true;
        }
        return false;
    }

    public void addMiniEnemy(int countdown)
    {
        if (miniEnemies == null)
        {
            miniEnemies = new List<MiniEnemy>();
        }
        boss.decreaseCountdown(countdown);
        Vector3 spawnPosition = boss.transform.position;
        GameObject miniEnemy = Instantiate(miniEnemyPrefab, spawnPosition, Quaternion.identity);
        MiniEnemy miniEnemyScript = miniEnemy.GetComponent<MiniEnemy>();
        miniEnemyScript.setCountdown(countdown);
        miniEnemyScript.LaunchFromBoss(spawnPosition);
        miniEnemies.Add(miniEnemyScript);
    }

    public void UnregisterMiniEnemy(MiniEnemy mini)
    {
        miniEnemies?.Remove(mini);
    }

    static int PickWeightedIndex(float[] weights, int count)
    {
        float total = 0f;
        for (int i = 0; i < count; i++)
            total += i < weights.Length ? weights[i] : 0f;

        if (total <= 0f)
            return UnityEngine.Random.Range(0, count);

        float roll = UnityEngine.Random.value * total;
        float cumulative = 0f;
        for (int i = 0; i < count; i++)
        {
            cumulative += i < weights.Length ? weights[i] : 0f;
            if (roll <= cumulative)
                return i;
        }

        return count - 1;
    }
}
