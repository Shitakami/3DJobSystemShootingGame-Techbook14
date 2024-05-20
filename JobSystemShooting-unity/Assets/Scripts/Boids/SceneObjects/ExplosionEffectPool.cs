using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Shitakami.Boids.SceneObjects
{
    public class ExplosionEffectPool : MonoBehaviour
    {
        [SerializeField] private EffectLifeTime _bulletPrefab;
        [SerializeField] private int _initialPoolSize;

        private Queue<EffectLifeTime> _pool = new Queue<EffectLifeTime>();

        public void Setup()
        {
            for (var i = 0; i < _initialPoolSize; i++)
            {
                var bullet = Instantiate(_bulletPrefab, transform);
                bullet.gameObject.SetActive(false);
                bullet.SetEffectDestroyedEvent(ReturnEffect);
                _pool.Enqueue(bullet);
            }
        }

        public bool TryGetEffect(out EffectLifeTime bullet)
        {
            if (_pool.Count > 0)
            {
                bullet = _pool.Dequeue();
                bullet.gameObject.SetActive(true);
                return true;
            }

            bullet = null;
            return false;
        }

        private void ReturnEffect(EffectLifeTime effectLifeTime)
        {
            effectLifeTime.gameObject.SetActive(false);
            _pool.Enqueue(effectLifeTime);
        }

        public ExplosionObstacle[] ApplyMovableObstacleToEffect()
        {
            return _pool
                .Select(poolObjectLifeTime => poolObjectLifeTime.gameObject.AddComponent<ExplosionObstacle>())
                .ToArray();
        }

        private void OnDestroy()
        {
            foreach (var bullet in _pool)
            {
                Destroy(bullet.gameObject);
            }

            _pool.Clear();
        }
    }
}