using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

[CreateAssetMenu(menuName = "Data/Level")]
public class LevelSO : ScriptableObject
{
    public List<EnemyWaveData> enemyWaves;
}
[System.Serializable]
public struct EnemyWaveData
{
    public int maxEnemyCount;
    public float enemySpawnInterval;
    public int maxBatchSpawnCount;
    public List<EnemyUnitData> enemies;
}
[System.Serializable]
public struct EnemyUnitData
{
    public int spawnWeight;
    public EnemySO unitInfo;
}