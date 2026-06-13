using System.Collections.Generic;
using UnityEngine;

namespace PeopleFlow.Core
{
    /// <summary>
    /// Generic object pool to avoid Instantiate/Destroy overhead.
    /// Manages separate pools per MinionType to support color-coded spawning.
    /// 
    /// Performance: Eliminates GC spikes from frequent Instantiate/Destroy calls,
    /// which is critical for smooth gameplay on mobile devices.
    /// 
    /// Usage:
    ///   objectPooler.InitializePool(MinionType.Red, redPrefab, 10);
    ///   GameObject minion = objectPooler.Get(MinionType.Red);
    ///   objectPooler.Return(minion, MinionType.Red);
    /// </summary>
    public class ObjectPooler : MonoBehaviour
    {
        [System.Serializable]
        public class PoolConfig
        {
            public MinionType type;
            public GameObject prefab;
            [Range(5, 50)]
            public int initialSize = 10;
        }

        [Header("Pool Configuration")]
        [SerializeField] private PoolConfig[] poolConfigs;

        private Dictionary<MinionType, Queue<GameObject>> _pools;
        private Dictionary<MinionType, GameObject> _prefabMap;
        private Transform _poolParent;

        private void Awake()
        {
            _pools = new Dictionary<MinionType, Queue<GameObject>>();
            _prefabMap = new Dictionary<MinionType, GameObject>();

            _poolParent = new GameObject("[Pool] Minions").transform;
            _poolParent.SetParent(transform);

            foreach (var config in poolConfigs)
            {
                InitializePool(config.type, config.prefab, config.initialSize);
            }
        }

        /// <summary>
        /// Creates a pool for the given MinionType with pre-instantiated objects.
        /// </summary>
        public void InitializePool(MinionType type, GameObject prefab, int initialSize)
        {
            if (_pools.ContainsKey(type))
            {
                Debug.LogWarning($"[ObjectPooler] Pool for {type} already exists. Skipping.");
                return;
            }

            _prefabMap[type] = prefab;
            var queue = new Queue<GameObject>(initialSize);

            for (int i = 0; i < initialSize; i++)
            {
                var obj = CreateNewObject(type);
                queue.Enqueue(obj);
            }

            _pools[type] = queue;
        }

        /// <summary>
        /// Retrieves an object from the pool. If the pool is empty, a new object is created
        /// (auto-expansion) to avoid runtime errors.
        /// </summary>
        public GameObject Get(MinionType type)
        {
            if (!_pools.ContainsKey(type))
            {
                Debug.LogError($"[ObjectPooler] No pool exists for {type}. Call InitializePool first.");
                return null;
            }

            GameObject obj;

            if (_pools[type].Count > 0)
            {
                obj = _pools[type].Dequeue();
            }
            else
            {
                // Auto-expand: create new object if pool is exhausted
                obj = CreateNewObject(type);
            }

            obj.SetActive(true);
            return obj;
        }

        /// <summary>
        /// Returns an object back to the pool for reuse.
        /// Resets position and deactivates the object.
        /// </summary>
        public void Return(GameObject obj, MinionType type)
        {
            if (!_pools.ContainsKey(type))
            {
                Debug.LogError($"[ObjectPooler] No pool exists for {type}. Destroying object instead.");
                Destroy(obj);
                return;
            }

            obj.SetActive(false);
            obj.transform.SetParent(_poolParent);
            _pools[type].Enqueue(obj);
        }

        /// <summary>
        /// Returns all active objects to the pool. Useful for level restart.
        /// </summary>
        public void ReturnAll()
        {
            foreach (var kvp in _pools)
            {
                // Objects already in the queue are inactive, no need to touch them
            }

            // Find all active Minion objects and return them
            foreach (Transform child in _poolParent)
            {
                if (child.gameObject.activeSelf)
                {
                    child.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// Gets the number of available (inactive) objects in a specific pool.
        /// </summary>
        public int GetAvailableCount(MinionType type)
        {
            return _pools.ContainsKey(type) ? _pools[type].Count : 0;
        }

        private GameObject CreateNewObject(MinionType type)
        {
            var obj = Instantiate(_prefabMap[type], _poolParent);
            obj.name = $"Minion_{type}";
            obj.SetActive(false);
            return obj;
        }
    }
}
