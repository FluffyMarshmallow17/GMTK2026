using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    float time;
    float playerTime;
    float bossTime;
    public Player player;
    public Boss boss;
    public List<MiniEnemy> miniEnemies;
    public GameObject miniEnemyPrefab;
    public GameObject map;
    public GameObject winScreen;
    public GameObject loseScreen;
    public bool levelEnded;

    public GameObject blockPrefab;
    public LevelData levelData;
    public AudioClip[] musicTracks;

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
    const float FadeToBlack = 2.55f;

    int numberSpriteCount;

    void Awake()
    {
        Time.timeScale = 1f;
        levelEnded = false;
        time = 0;
        playerTime = 0;
        bossTime = 0;
        miniEnemies = new List<MiniEnemy>();
        numberSpriteCount = blockPrefab.GetComponent<Block>().NumberSpriteCount;
        player.setCountdown(levelData.initialPlayerCount);
        boss.setCountdown(levelData.initialBossCount);
    }

    void Start()
    {
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
        if (player != null)
            player.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;

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

        // Let shake ease off as we sink into black.
        CameraShake.ReleaseCinematic(0.55f);

        bool blackDone = false;
        if (fade != null)
            fade.FadeToBlack(FadeToBlack, ScreenFade.Ease.EaseOutCubic, () => blackDone = true);
        else
            blackDone = true;

        while (!blackDone)
            yield return null;

        Time.timeScale = 0f;
        showScreen?.Invoke();
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
        if (levelData.maxBlocks <= 0)
            return Mathf.Max(0.01f, levelData.blockRate);

        int count = CountBlocks();
        if (count >= levelData.maxBlocks)
            return float.PositiveInfinity;

        float fill = (float)count / levelData.maxBlocks;
        return Mathf.Max(0.01f, levelData.blockRate / (1f - fill));
    }

    static int CountBlocks()
    {
        return FindObjectsByType<Block>(FindObjectsSortMode.None).Length;
    }

    public void spawnBlock()
    {
        if (levelData.maxBlocks > 0 && CountBlocks() >= levelData.maxBlocks)
            return;

        float minRadius = Mathf.Min(levelData.spawnMin, levelData.spawnMax);
        float maxRadius = Mathf.Max(levelData.spawnMin, levelData.spawnMax);
        float radius = UnityEngine.Random.Range(minRadius, maxRadius);
        float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        Vector3 spawnPosition = new Vector3(
            Mathf.Cos(angle) * radius,
            Mathf.Sin(angle) * radius,
            0
        );

        Block block = Instantiate(blockPrefab, spawnPosition, Quaternion.identity)
            .GetComponent<Block>();

        float categoryTotal = levelData.numberProbability + levelData.operationProbability;
        bool spawnNumber = categoryTotal <= 0f
            || UnityEngine.Random.value < levelData.numberProbability / categoryTotal;

        if (spawnNumber)
            block.SetNumber(PickWeightedIndex(levelData.numberProbabilities, numberSpriteCount));
        else
            block.SetOperation((operationType)PickWeightedIndex(
                new[] { levelData.addProbability, levelData.subtractProbability, levelData.multiplyProbability, levelData.divideProbability, levelData.decayProbability, levelData.growProbability },
                6));

        if (levelData.includeMiniEnemies)
            addMiniEnemy(UnityEngine.Random.Range(5, 15));
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
