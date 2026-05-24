using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace Game
{
    public class PooledSpawnService
    {
        private readonly Dictionary<string, IObjectPool<PooledObject>> _pools = new();
        private PooledObject _currentObject = null;

        public PooledObject SpawnObject(GameObject gameObject, string poolName, Vector3 spawnPosition, Vector3 spawnEuler, float lifeTime = 0)
        {
            if (!_pools.ContainsKey(poolName))
            {
                IObjectPool<PooledObject> pool = new ObjectPool<PooledObject>(
                    createFunc: () =>
                    {
                        var spawnedObj = GameObject.Instantiate(gameObject);

                        // Get or add component
                        if (!spawnedObj.TryGetComponent<PooledObject>(out var pooledObject)) pooledObject = spawnedObj.AddComponent<PooledObject>();

                        return pooledObject;
                    },

                    actionOnGet: (pooledObject) =>
                    {
                        pooledObject.gameObject.SetActive(true);
                        // Не сбрасываем позицию здесь, она будет установлена через SetPositionAndRotation
                    },

                    actionOnRelease: (pooledObject) =>
                    {
                        pooledObject.transform.position = Vector3.zero;
                        pooledObject.transform.rotation = Quaternion.identity; // добавить
                        pooledObject.gameObject.SetActive(false);
                        pooledObject.StopAllCoroutines();
                    },

                    actionOnDestroy: (pooledObject) =>
                    {
                        GameObject.Destroy(pooledObject.gameObject);
                    },

                    defaultCapacity: 30
                );

                _pools.Add(poolName, pool);
            }

            _currentObject = _pools[poolName].Get();

            _currentObject.Initialize(_pools[poolName], lifeTime);
            _currentObject.transform.SetPositionAndRotation(spawnPosition, Quaternion.Euler(spawnEuler));

            return _currentObject;
        }
    }
}