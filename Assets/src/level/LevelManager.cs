using System;
using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    float time; 
    public Player player; 
    public Boss boss;
    public List<MiniEnemy> miniEnemies;

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
        }

        
        
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