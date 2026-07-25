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
    public GameObject map;
    public GameObject winScreen;
    public GameObject loseScreen;

    public GameObject blockPrefab;

    void Awake()
    {
        time = 0;
        miniEnemies = new List<MiniEnemy>();
    }

    void Start()
    {
        map.GetComponent<Map>().snapToCountdown(GetTotalCountdown());
    }

    void FixedUpdate()
    {
        time += Time.deltaTime;
        if (time >= 1) {
           time = 0;
           decreaseCountdown();

           if (player.getCountdown() <= 0)
           {
               // Player loses
               string currentLevel = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Remove(0, 6); // Remove "Level " prefix
               print(currentLevel);
               int currentLevelIndex = int.Parse(currentLevel);
               loseScreen.GetComponent<LoseScreen>().ShowLoseScreen(currentLevelIndex);
           }
           else if (boss.getCountdown() <= 0)
           {
               // Player wins
               string currentLevel = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.Remove(0, 6); // Remove "Level " prefix
               int currentLevelIndex = int.Parse(currentLevel);
               PlayerPrefs.SetInt("LevelsUnlocked", Math.Max(PlayerPrefs.GetInt("LevelsUnlocked", 1), currentLevelIndex + 1));
               winScreen.GetComponent<WinScreen>().ShowWinScreen(currentLevelIndex);
           }
           spawnBlock();
        }
        
        map.GetComponent<Map>().resizeMap(GetTotalCountdown());
    }

    int GetTotalCountdown()
    {
        int totalCountdown = player.getCountdown() + boss.getCountdown();
        if (miniEnemies != null) {
            foreach (MiniEnemy enemy in miniEnemies)
            {
                totalCountdown += enemy.getCountdown();
            }
        }
        return totalCountdown;
    }

    public void spawnBlock()
    {
        Vector3 spawnPosition = new Vector3(
            UnityEngine.Random.Range(-15f, 15f),
            UnityEngine.Random.Range(-15f, 15f),
            0
        );

        Instantiate(blockPrefab, spawnPosition, Quaternion.identity);
        // addMiniEnemy(UnityEngine.Random.Range(5, 15), (spawnPosition - 10 * Vector3.up));
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

    public void addMiniEnemy(int countdown, Vector3 spawnPosition = default(Vector3))
    {
        if (miniEnemies == null)
        {
            miniEnemies = new List<MiniEnemy>();
        }
        boss.decreaseCountdown(countdown);
        GameObject miniEnemy = Instantiate(miniEnemyPrefab, spawnPosition, Quaternion.identity);
        MiniEnemy miniEnemyScript = miniEnemy.GetComponent<MiniEnemy>();
        miniEnemyScript.setCountdown(countdown);
        miniEnemies.Add(miniEnemyScript);
    }
}