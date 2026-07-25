using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;

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

    public GameObject blockPrefab;
    public LevelData levelData;

    int numberSpriteCount;

    void Awake()
    {
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
    }

    void FixedUpdate()
    {
        time += Time.deltaTime;
        playerTime += Time.deltaTime;
        bossTime += Time.deltaTime;
        if (playerTime >= (1 * player.getRate())) {
           playerTime = 0;
           player.decreaseCountdown();
            if (player.getCountdown() <= 0)
            {
               // Player loses
               string currentLevel = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Remove(0, 6); // Remove "Level " prefix
               print(currentLevel);
               int currentLevelIndex = int.Parse(currentLevel);
               loseScreen.GetComponent<LoseScreen>().ShowLoseScreen(currentLevelIndex);
            }
        }
        if (bossTime >= (1 * boss.getRate()))
        {
            bossTime = 0;
            boss.decreaseCountdown();
            if (boss.getCountdown() <= 0)
            {
               // Player wins
               string currentLevel = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Remove(0, 6); // Remove "Level " prefix
               int currentLevelIndex = int.Parse(currentLevel);
               PlayerPrefs.SetInt("LevelsUnlocked", Math.Max(PlayerPrefs.GetInt("LevelsUnlocked", 1), currentLevelIndex + 1));
               winScreen.GetComponent<WinScreen>().ShowWinScreen(currentLevelIndex);
            }
        }
        float spawnInterval = GetBlockSpawnInterval();
        if (time >= spawnInterval) {
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

    int GetTotalCountdown()
    {
        int totalCountdown = player.getCountdown() + boss.getCountdown();
        if (miniEnemies != null) {
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
