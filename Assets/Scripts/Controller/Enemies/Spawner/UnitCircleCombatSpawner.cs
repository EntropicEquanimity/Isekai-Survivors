using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NaughtyAttributes;

public class UnitCircleCombatSpawner : MonoBehaviour
{
    [BoxGroup("Spawner")] [Required] public EnemySO enemyPrefab;
    [BoxGroup("Spawner")] public float spawnCooldown = 20f;
    [BoxGroup("Spawner")] public float initialCooldown = 0;
    [BoxGroup("Spawner")] public int spawnCount = 10;
    [BoxGroup("Spawner")] public int maxEnemies = 10;

    [BoxGroup("Spawner")] public bool unitsRotateAroundCenter;
    [BoxGroup("Spawner")] [ShowIf("unitsRotateAroundCenter")] public float rotationSpeed = 0f;
    [BoxGroup("Spawner")] public float distanceFromCenter = 2f;
    [BoxGroup("Spawner")] public float rotationalOffset = 0;
    [BoxGroup("Spawner")] [ReadOnly] private float _spawnCD = 2;
    [BoxGroup("Spawner")] [ReadOnly] public List<Enemy> enemies;
    [BoxGroup("Spawner")] [ReadOnly] public Vector3[] positions;

    private void Awake()
    {
        _spawnCD = initialCooldown;
        enemies = new List<Enemy>();
    }
    public void FixedUpdate()
    {
        if(enemyPrefab == null || enemies.Count >= maxEnemies) { return; }
        _spawnCD -= Time.fixedDeltaTime;

        if (_spawnCD <= 0f)
        {
            SpawnEntities();
            _spawnCD = spawnCooldown;
        }
    }

    public void UpdatePositions()
    {

    }
    public void SpawnEntities()
    {
        if (enemies.Count >= maxEnemies) { return; }

        positions = GetSpawnPositions(GetAllowedSpawnCount);

        for (int i = 0; i < GetAllowedSpawnCount; i++)
        {
            Debug.Log(positions[i]);
            enemies.Add(Instantiate(enemyPrefab.unitPrefab, positions[i], Quaternion.identity).GetComponent<Enemy>());
            enemies[i].followTargetPositionInstead = true;
        }
    }
    
    #region Getters
    public int GetAllowedSpawnCount => spawnCount - (maxEnemies - enemies.Count);
    public Vector3[] GetSpawnPositions(int spawnCount, float offset = 0)
    {
        float angleDist = 360 / spawnCount;
        Vector3 startPos = transform.position;
        Vector3[] positions = new Vector3[spawnCount];
        for (int i = 0; i < spawnCount; i++)
        {
            float angle = i * angleDist + rotationalOffset + offset;
            float rad = angle * Mathf.PI / 180;
            float x = startPos.x + distanceFromCenter * Mathf.Cos(rad);
            float y = startPos.y + distanceFromCenter * Mathf.Sin(rad);
            positions[i] = new Vector3(x, y, 0);
        }
        return positions;
    }
    #endregion
    private void OnDrawGizmosSelected()
    {
        Vector3[] positions = GetSpawnPositions(maxEnemies);
        for (int i = 0; i < positions.Length; i++)
        {
            Gizmos.DrawSphere(positions[i], 0.1f);
        }
    }
}
