using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Shitakami.Boids.SceneObjects
{
    public class ExplosionEffectPool : MonoBehaviour
    {
        [SerializeField] private EffectLifeTime _explosionEffectPrefab;
        [SerializeField] private int _initialPoolSize;

        private Queue<EffectLifeTime> _pool = new Queue<EffectLifeTime>();
        private ExplosionObstacle[] _explosionObstacleArray;
        
        public IReadOnlyList<ExplosionObstacle> ExplosionObstacleArray => _explosionObstacleArray; 
        
        public void Setup()
        {
            _explosionObstacleArray = new ExplosionObstacle[_initialPoolSize];
            
            for (var i = 0; i < _initialPoolSize; i++)
            {
                var explosionEffect = Instantiate(_explosionEffectPrefab, transform);
                explosionEffect.gameObject.SetActive(false);
                explosionEffect.SetEffectDestroyedEvent(ReturnEffect);
                _explosionObstacleArray[i] = explosionEffect.gameObject.AddComponent<ExplosionObstacle>();
                _pool.Enqueue(explosionEffect);
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