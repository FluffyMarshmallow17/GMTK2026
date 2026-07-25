using UnityEngine;

[CreateAssetMenu(fileName = "LevelData", menuName = "Game/Level Data")]
public class LevelData : ScriptableObject
{
    public int initialPlayerCount;
    public int initialBossCount;
    public bool includeMiniEnemies;

    [Range(0f, 1f)] public float numberProbability;
    [Range(0f, 1f)] public float operationProbability;

    public float[] numberProbabilities;

    public float addProbability;
    public float subtractProbability;
    public float multiplyProbability;
    public float divideProbability;
}
