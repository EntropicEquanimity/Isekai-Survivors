using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    [BoxGroup("Enemies")] public LevelSO levelData;
    [BoxGroup("Enemies")] public Transform[] spawnPoints;

    [BoxGroup("Enemies")] [ReadOnly] public EnemyWaveData currentWaveData;
    [BoxGroup("Enemies")] [ReadOnly] public List<Enemy> enemies = new List<Enemy>();
    [BoxGroup("Enemies")] [ReadOnly] public float spawnCooldown;
    [BoxGroup("Enemies")] [ReadOnly] public int currentWave = 0;
    [BoxGroup("Enemies")] [ReadOnly] public int maxSize;
    [BoxGroup("Enemies")] [ReadOnly] public int maxSpawnAtOnce;
    [BoxGroup("Enemies")] [ReadOnly] public float spawnInterval = 0.5f;

    public int MaxSize { get => maxSize; set => maxSize = value + Mathf.RoundToInt(GameManager.Instance.gameTime / 10); }

    public static EnemyManager Instance;
    private void Awake()
    {
        if (Instance == null) { Instance = this; }

        enemies = new List<Enemy>();
        currentWave = 0;
    }
    private void FixedUpdate()
    {
        spawnCooldown -= Time.fixedDeltaTime;
        if (currentWave != GetCurrentWave())
        {
            enemies.Clear();
            currentWave = GetCurrentWave();
            currentWaveData = levelData.enemyWaves[currentWave - 1];
            maxSize = currentWaveData.maxEnemyCount;
            maxSpawnAtOnce = currentWaveData.maxBatchSpawnCount;
            spawnInterval = currentWaveData.enemySpawnInterval;
        }

        if (spawnCooldown <= 0)
        {
            if (MaxSize > GetNumberOfEnemies())
            {
                int spawnNum = Mathf.Clamp(MaxSize - GetNumberOfEnemies(), 0, maxSpawnAtOnce);
                for (int i = 0; i < spawnNum; i++)
                {
                    EnemyUnitData enemyData = GetEnemyToSpawn();
                    Enemy enemy = Instantiate(enemyData.unitInfo.unitPrefab, transform).GetComponent<Enemy>();
                    //enemy.Initialize(enemy.baseStats.entityStats);
                    enemy.transform.position = GameManager.Instance.player.transform.position + spawnPoints[Random.Range(0, spawnPoints.Length)].position + (Vector3)Random.insideUnitCircle * 0.5f;
                    enemies.Add(enemy);
                }
            }
            //for (int i = 0; i < MaxSize - GetNumberOfEnemies(); i++)
            //{
            //    for (int e = 0; e < enemies.Count; e++)
            //    {
            //        if (!enemies[e].gameObject.activeInHierarchy)
            //        {
            //            enemies[e].gameObject.SetActive(true);
            //            enemies[e].entitySpriteRenderer.sprite = enemies[e].baseStats.entitySprite;
            //            enemies[e].Initialize(enemies[e].baseStats.entityStats);
            //            enemies[e].transform.position = GameManager.Instance.player.transform.position + spawnPoints[Random.Range(0, spawnPoints.Length)].position;
            //        }
            //    }
            //}
            spawnCooldown = spawnInterval;
        }
    }
    public EnemyUnitData GetEnemyToSpawn()
    {
        int weight = 0;

        for (int i = 0; i < currentWaveData.enemies.Count; i++)
        {
            weight += currentWaveData.enemies[i].spawnWeight;
        }
        int roll = Random.Range(0, weight);
        for (int i = 0; i < currentWaveData.enemies.Count; i++)
        {
            weight -= currentWaveData.enemies[i].spawnWeight;
            if (weight <= roll)
            {
                return currentWaveData.enemies[i];
            }
        }
        //Debug.LogError("Error calculating weights for choosing enemy to spawn!");
        return currentWaveData.enemies[0];
    }
    public void EnemyDeath(Enemy enemy)
    {
        if (enemies.Contains(enemy))
        {
            enemies.Remove(enemy);
            Destroy(enemy.gameObject);
        }
    }
    public int GetNumberOfEnemies()
    {
        int num = 0;
        for (int i = 0; i < enemies.Count; i++)
        {
            if (enemies[i].gameObject.activeInHierarchy)
            {
                num++;
            }
        }
        return num;
    }
    public int GetCurrentWave()
    {
        return Mathf.FloorToInt(GameManager.Instance.gameTime / levelData.waveDuration) + 1;
    }
    public void CheckIfAllWavesCompleted()
    {
        if (levelData.enemyWaves.Count <= GetCurrentWave())
        {
            ///Out of waves.
            for (int i = 0; i < enemies.Count; i++)
            {
                enemies[i].Die();
            }

        }
        else
        {
            ///More waves.
        }
    }
}
