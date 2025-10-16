using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

public class UnitCircleCombatSpawner : MonoBehaviour
{
    [BoxGroup("Spawner")] [Required] public EnemySO enemyPrefab;
    [BoxGroup("Spawner")] public float spawnCooldown = 20f;
    [BoxGroup("Spawner")] public float initialCooldown = 0;
    [BoxGroup("Spawner")] public int spawnCount = 10;
    [BoxGroup("Spawner")] public int maxEnemies = 10;
    [BoxGroup("Spawner")] public bool continuousSpawn = false;

    [BoxGroup("Spawner")] public bool unitsRotateAroundCenter;
    [BoxGroup("Spawner")] [ShowIf("unitsRotateAroundCenter")] public float rotationSpeed = 0f;
    [BoxGroup("Spawner")] public float distanceFromCenter = 2f;
    [BoxGroup("Spawner")] public float rotationalOffset = 0;
    [BoxGroup("Spawner")] [ReadOnly] [SerializeField] private float _spawnCD = 2;
    [BoxGroup("Spawner")] [ReadOnly] [SerializeField] private float _rot = 0;
    [BoxGroup("Spawner")] [ReadOnly] public List<Enemy> enemies;
    [BoxGroup("Spawner")] [ReadOnly] public Vector3[] positions;

    private void Awake()
    {
        _spawnCD = initialCooldown;
        enemies = new List<Enemy>();
    }
    private void Start()
    {
        SpawnEntities();
    }
    public void FixedUpdate()
    {
        UpdatePositions();
        if (unitsRotateAroundCenter) { _rot += Time.fixedDeltaTime * rotationSpeed; }
        if (enemyPrefab == null || enemies.Count >= maxEnemies || !continuousSpawn) { return; }

        _spawnCD -= Time.fixedDeltaTime;

        if (_spawnCD <= 0f)
        {
            SpawnEntities();
            _spawnCD = spawnCooldown;
        }
    }
    private void OnDestroy()
    {
        for (int i = 0; i < enemies.Count; i++)
        {
            enemies[i].followTargetPositionInstead = false;
        }
    }
    public void UpdatePositions()
    {
        if(enemies.Count < 1) { return; }
        Vector3[] positions = GetPositions(enemies.Count, _rot);
        for (int i = 0; i < enemies.Count; i++)
        {
            enemies[i].targetPosition = positions[i];
        }
    }
    public void SpawnEntities()
    {
        if (enemies.Count >= maxEnemies) { return; }
        positions = GetPositions(spawnCount);
        for (int i = 0; i < spawnCount; i++)
        {
            enemies.Add(Instantiate(enemyPrefab.unitPrefab, positions[i], Quaternion.identity).GetComponent<Enemy>());
            enemies[i].followTargetPositionInstead = true;
        }
    }
    
    #region Getters
    public Vector3[] GetPositions(int posCount, float offset = 0)
    {
        float angleDist = 360 / posCount;
        Vector3 startPos = transform.position;
        Vector3[] positions = new Vector3[posCount];
        for (int i = 0; i < posCount; i++)
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
        Vector3[] positions = GetPositions(maxEnemies, _rot);
        for (int i = 0; i < positions.Length; i++)
        {
            Gizmos.DrawSphere(positions[i], 0.1f);
        }
    }
}
