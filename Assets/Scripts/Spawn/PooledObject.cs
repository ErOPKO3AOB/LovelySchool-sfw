using System.Collections;
using UnityEngine;
using UnityEngine.Pool;

namespace Game
{
    public class PooledObject : MonoBehaviour
    {
        private IObjectPool<PooledObject> _myPool;

        public void Initialize(IObjectPool<PooledObject> myPool, float lifetime = 0)
        {
            _myPool = myPool;

            if (lifetime > 0)
            {
                StartCoroutine(LifetimeRoutine(lifetime));
            }
        }

        private IEnumerator LifetimeRoutine(float lifeTime)
        {
            yield return new WaitForSeconds(lifeTime);

            ReturnToPool();
        }

        public void ReturnToPool()
        {
            _myPool.Release(this);
        }
    }
}