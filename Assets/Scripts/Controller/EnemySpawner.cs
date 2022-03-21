using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [BoxGroup("Enemies")] public GameObject[] enemyPrefabs;
    [BoxGroup("Enemies")] public LevelSO levelData;
    [BoxGroup("Enemies")] [ReadOnly] public List<SimpleEnemy> enemies = new List<SimpleEnemy>();
    [BoxGroup("Enemies")] [SerializeField] protected int _maxSize;
    [BoxGroup("Enemies")] public int maxSpawnAtOnce;
    [BoxGroup("Enemies")] public float spawnInterval = 0.5f;
    [BoxGroup("Enemies")] [ReadOnly] public float spawnCooldown;
    [BoxGroup("Enemies")] public Transform[] spawnPoints;

    public int MaxSize { get => _maxSize; set => _maxSize = value + Mathf.RoundToInt(GameManager.Instance.gameTime / 10); }

    public static EnemySpawner Instance;
    private void Awake()
    {
        if (Instance == null) { Instance = this; }
    }
    private void FixedUpdate()
    {
        spawnCooldown -= Time.fixedDeltaTime;

        if (spawnCooldown <= 0)
        {
            if (enemies.Count < MaxSize)
            {
                int spawnNum = Mathf.Min(MaxSize - enemies.Count, maxSpawnAtOnce);
                for (int i = 0; i < spawnNum; i++)
                {
                    SimpleEnemy enemy = Instantiate(enemyPrefabs[Random.Range(0, enemyPrefabs.Length)], transform).GetComponent<SimpleEnemy>();
                    enemy.Initialize(enemy.baseStats.entityStats);
                    enemy.transform.position = GameManager.Instance.player.transform.position + spawnPoints[Random.Range(0, spawnPoints.Length)].position;
                    enemies.Add(enemy);
                }
            }
            for (int i = 0; i < MaxSize - GetNumberOfEnemies(); i++)
            {
                for (int e = 0; e < enemies.Count; e++)
                {
                    if (!enemies[e].gameObject.activeInHierarchy)
                    {
                        enemies[e].gameObject.SetActive(true);
                        enemies[e].entitySpriteRenderer.sprite = enemies[e].baseStats.entitySprite;
                        enemies[e].Initialize(enemies[e].baseStats.entityStats);
                        enemies[e].transform.position = GameManager.Instance.player.transform.position + spawnPoints[Random.Range(0, spawnPoints.Length)].position;
                    }
                }
            }
            spawnCooldown = spawnInterval;
        }
    }
    public int GetNumberOfEnemies()
    {
        int num = 0;
        for (int i = 0; i < enemies.Count; i++)
        {
            if (!enemies[i].gameObject.activeInHierarchy)
            {
                num++;
            }
        }
        return num;
    }
    public int GetCurrentWave()
    {
        return Mathf.FloorToInt(GameManager.Instance.gameTime / 60);
    }
}
