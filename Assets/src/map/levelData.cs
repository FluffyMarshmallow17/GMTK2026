using UnityEngine;

[CreateAssetMenu(fileName = "LevelData", menuName = "Game/Level Data")]
public class LevelData : ScriptableObject
{
    public int initialPlayerCount;
    public int initialBossCount;
    public bool includeMiniEnemies;
    [Tooltip("Seconds between mini-enemy spawns (when Include Mini Enemies is on).")]
    public float miniEnemySpawnRate = 5f;
    public bool changingBorders;
    public bool spawnZones;
    [Tooltip("Seconds between zone spawn attempts.")]
    public float zoneSpawnRate;
    [Tooltip("Seconds a zone lasts before expiring (0 = never).")]
    public float zoneLifespan;

    [Header("Zones")]
    public int zoneSizeMin;   // radius
    public int zoneSizeMax;
    public int zoneCostMin;
    public int zoneCostMax;
    public int zoneBlocksMin;
    public int zoneBlocksMax;

    public float hardcodeRadius;
    public int maxBlocks;

    public int spawnMin; // both zone and blocks
    public int spawnMax; // both zone and blocks
    public float blockRate;

    [Range(0f, 1f)] public float numberProbability;
    [Range(0f, 1f)] public float operationProbability;

    public float[] numberProbabilities;

    public float addProbability;
    public float subtractProbability;
    public float multiplyProbability;
    public float divideProbability;
    public float decayProbability;
    public float growProbability;
}
