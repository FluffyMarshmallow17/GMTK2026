using System;
using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    float time; 
    public Player player; 
    public Boss boss;
    public List<MiniEnemy> miniEnemies;

    public GameObject blockPrefab;

    void Awake()
    {
        time = 0;
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
            UnityEngine.Random.Range(-10f, 10f),
            UnityEngine.Random.Range(-10f, 10f),
            0
        );

        Instantiate(blockPrefab, spawnPosition, Quaternion.identity);
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
}