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