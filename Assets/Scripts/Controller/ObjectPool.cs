using System;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
    #region Singleton Pattern
    private static ObjectPool _instance;
    public static ObjectPool Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<ObjectPool>();
                if (_instance == null)
                {
                    GameObject obj = new GameObject("ObjectPool");
                    _instance = obj.AddComponent<ObjectPool>();
                    DontDestroyOnLoad(obj);
                }
            }
            return _instance;
        }
    }
    #endregion

    [System.Serializable]
    public class Pool
    {
        public string key;
        public GameObject prefab;
        public int initialSize = 10;
        public int maxSize = 100;
        public Transform parent;
    }

    [Header("Pool Configuration")]
    public List<Pool> predefinedPools = new List<Pool>();

    private Dictionary<string, Queue<GameObject>> poolDictionary;
    private Dictionary<string, Pool> poolConfigs;
    private Dictionary<GameObject, string> objectToKeyMap;

    #region MonoBehaviour Methods
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        Initialize();
    }

    private void OnDestroy()
    {
        ClearAllPools();
    }

    private void OnApplicationQuit()
    {
        ClearAllPools();
    }
    #endregion

    #region Initialization
    private void Initialize()
    {
        poolDictionary = new Dictionary<string, Queue<GameObject>>();
        poolConfigs = new Dictionary<string, Pool>();
        objectToKeyMap = new Dictionary<GameObject, string>();

        // Initialize predefined pools
        foreach (var poolConfig in predefinedPools)
        {
            CreatePool(poolConfig.key, poolConfig.prefab, poolConfig.initialSize, poolConfig.maxSize, poolConfig.parent);
        }
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// Creates a new object pool
    /// </summary>
    public void CreatePool(string key, GameObject prefab, int initialSize = 10, int maxSize = 100, Transform parent = null)
    {
        if (poolDictionary.ContainsKey(key))
        {
            Debug.LogWarning($"Pool with key '{key}' already exists!");
            return;
        }

        if (prefab == null)
        {
            Debug.LogError("Cannot create pool with null prefab!");
            return;
        }

        Queue<GameObject> objectPool = new Queue<GameObject>();

        // Store pool configuration
        poolConfigs[key] = new Pool
        {
            key = key,
            prefab = prefab,
            initialSize = initialSize,
            maxSize = maxSize,
            parent = parent
        };

        // Pre-instantiate objects
        for (int i = 0; i < initialSize; i++)
        {
            GameObject obj = CreateNewObject(key, prefab, parent);
            objectPool.Enqueue(obj);
        }

        poolDictionary.Add(key, objectPool);
        Debug.Log($"Created pool '{key}' with {initialSize} initial objects");
    }

    /// <summary>
    /// Spawns an object from the pool
    /// </summary>
    public GameObject Spawn(string key, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (!poolDictionary.ContainsKey(key))
        {
            Debug.LogError($"Pool with key '{key}' doesn't exist!");
            return null;
        }

        Queue<GameObject> pool = poolDictionary[key];
        Pool config = poolConfigs[key];

        GameObject objectToSpawn;

        // If pool is empty and we haven't reached max size, create a new object
        if (pool.Count == 0 && CanGrowPool(key))
        {
            objectToSpawn = CreateNewObject(key, config.prefab, config.parent);
        }
        else if (pool.Count > 0)
        {
            objectToSpawn = pool.Dequeue();
        }
        else
        {
            Debug.LogWarning($"Pool '{key}' is at maximum capacity ({config.maxSize})!");
            return null;
        }

        // Set up the object
        objectToSpawn.transform.position = position;
        objectToSpawn.transform.rotation = rotation;

        if (parent != null)
        {
            objectToSpawn.transform.SetParent(parent);
        }
        else if (config.parent != null)
        {
            objectToSpawn.transform.SetParent(config.parent);
        }

        objectToSpawn.SetActive(true);

        // Notify components that object was spawned
        IPoolable[] poolables = objectToSpawn.GetComponentsInChildren<IPoolable>();
        foreach (IPoolable poolable in poolables)
        {
            poolable.OnSpawn();
        }

        return objectToSpawn;
    }

    /// <summary>
    /// Returns an object to the pool
    /// </summary>
    public void Despawn(GameObject obj)
    {
        if (obj == null) return;

        if (!objectToKeyMap.TryGetValue(obj, out string key))
        {
            Debug.LogWarning("Object doesn't belong to any pool! Destroying instead.");
            Destroy(obj);
            return;
        }

        if (!poolDictionary.ContainsKey(key))
        {
            Debug.LogWarning($"Pool '{key}' doesn't exist! Destroying object.");
            Destroy(obj);
            return;
        }

        Pool config = poolConfigs[key];

        // If pool is at max capacity, destroy the object instead
        if (poolDictionary[key].Count >= config.maxSize)
        {
            Debug.LogWarning($"Pool '{key}' is at maximum capacity! Destroying object.");
            Destroy(obj);
            objectToKeyMap.Remove(obj);
            return;
        }

        // Reset object state
        obj.SetActive(false);
        obj.transform.SetParent(config.parent);
        obj.transform.position = Vector3.zero;
        obj.transform.rotation = Quaternion.identity;

        // Notify components that object was despawned
        IPoolable[] poolables = obj.GetComponentsInChildren<IPoolable>();
        foreach (IPoolable poolable in poolables)
        {
            poolable.OnDespawn();
        }

        // Return to pool
        poolDictionary[key].Enqueue(obj);
    }

    /// <summary>
    /// Removes a pool and all its objects
    /// </summary>
    public void RemovePool(string key)
    {
        if (!poolDictionary.ContainsKey(key))
        {
            Debug.LogWarning($"Pool with key '{key}' doesn't exist!");
            return;
        }

        Queue<GameObject> pool = poolDictionary[key];

        // Destroy all objects in the pool
        while (pool.Count > 0)
        {
            GameObject obj = pool.Dequeue();
            if (obj != null)
            {
                objectToKeyMap.Remove(obj);
                Destroy(obj);
            }
        }

        poolDictionary.Remove(key);
        poolConfigs.Remove(key);

        Debug.Log($"Removed pool '{key}'");
    }

    /// <summary>
    /// Clears all pools and destroys all objects
    /// </summary>
    public void ClearAllPools()
    {
        foreach (var kvp in poolDictionary)
        {
            Queue<GameObject> pool = kvp.Value;
            while (pool.Count > 0)
            {
                GameObject obj = pool.Dequeue();
                if (obj != null)
                {
                    Destroy(obj);
                }
            }
        }

        poolDictionary.Clear();
        poolConfigs.Clear();
        objectToKeyMap.Clear();

        Debug.Log("All pools cleared");
    }

    /// <summary>
    /// Gets the current size of a pool
    /// </summary>
    public int GetPoolSize(string key)
    {
        if (poolDictionary.ContainsKey(key))
        {
            return poolDictionary[key].Count;
        }
        return -1;
    }

    /// <summary>
    /// Checks if a pool exists
    /// </summary>
    public bool PoolExists(string key)
    {
        return poolDictionary.ContainsKey(key);
    }
    #endregion

    #region Private Methods
    private GameObject CreateNewObject(string key, GameObject prefab, Transform parent = null)
    {
        GameObject obj = Instantiate(prefab);
        obj.name = $"{prefab.name}_Pooled";
        obj.SetActive(false);

        if (parent != null)
        {
            obj.transform.SetParent(parent);
        }

        // Track which pool this object belongs to
        objectToKeyMap[obj] = key;

        return obj;
    }

    private bool CanGrowPool(string key)
    {
        if (!poolConfigs.ContainsKey(key)) return false;

        Pool config = poolConfigs[key];
        int totalObjects = GetTotalObjectsInPool(key);

        return totalObjects < config.maxSize;
    }

    private int GetTotalObjectsInPool(string key)
    {
        int count = poolDictionary[key].Count;

        // Count active objects that belong to this pool
        foreach (var kvp in objectToKeyMap)
        {
            if (kvp.Value == key && kvp.Key.activeInHierarchy)
            {
                count++;
            }
        }

        return count;
    }
    #endregion
}

/// <summary>
/// Interface for objects that need to know when they're spawned/despawned
/// </summary>
public interface IPoolable
{
    void OnSpawn();
    void OnDespawn();
}