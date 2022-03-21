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
    public int enemySpawnInterval;
    public int maxBatchSpawnCount;
    public List<EnemyUnitData> enemies;
}
[System.Serializable]
public struct EnemyUnitData
{
    public bool unlimitedSpawn;
    [HideIf("unlimitedSpawn")] public int spawnCount;
    public EnemySO unitInfo;
}