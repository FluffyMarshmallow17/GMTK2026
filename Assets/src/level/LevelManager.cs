using System;
using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    float time; 
    public Player player; 
    public Boss boss;
    public List<MiniEnemy> miniEnemies;
    public GameObject miniEnemyPrefab;

    public GameObject blockPrefab;
    public LevelData levelData;

    int numberSpriteCount;

    void Awake()
    {
        time = 0;
        miniEnemies = new List<MiniEnemy>();
        numberSpriteCount = blockPrefab.GetComponent<Block>().NumberSpriteCount;
        player.setCountdown(levelData.initialPlayerCount);
        boss.setCountdown(levelData.initialBossCount);
    }

    void Update()
    {
        time += Time.deltaTime;
        if (time >= 1) {
           time = 0;
           decreaseCountdown(); 
           spawnBlock();
        }
        
        
    }

    public void spawnBlock()
    {
        Vector3 spawnPosition = new Vector3(
            UnityEngine.Random.Range(-15f, 15f),
            UnityEngine.Random.Range(-15f, 15f),
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
                new[] { levelData.addProbability, levelData.subtractProbability, levelData.multiplyProbability, levelData.divideProbability },
                4));

        if (levelData.includeMiniEnemies)
            addMiniEnemy(UnityEngine.Random.Range(5, 15));
    }

    public void decreaseCountdown()
    {
        player.decreaseCountdown();
        boss.decreaseCountdown();
        if (miniEnemies != null) {
            foreach (MiniEnemy enemy in miniEnemies)
            {
                enemy.decreaseCountdown();
            }
        }
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
