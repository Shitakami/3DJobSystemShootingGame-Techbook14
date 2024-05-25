using System.Collections.Generic;
using UnityEngine;

namespace Shitakami.Boids.SceneObjects
{
    public class BulletPool : MonoBehaviour
    {
        [SerializeField] private Bullet _bulletPrefab;
        [SerializeField] private int _initialPoolSize;

        private Queue<Bullet> _pool = new Queue<Bullet>();
        private Bullet[] _bulletArray;
        
        public IReadOnlyList<Bullet> BulletArray => _bulletArray;

        public void Setup()
        {
            for (var i = 0; i < _initialPoolSize; i++)
            {
                var bullet = Instantiate(_bulletPrefab, transform);
                bullet.gameObject.SetActive(false);
                bullet.SetBulletDestroyedEvent(ReturnBullet);
                _pool.Enqueue(bullet);
            }

            _bulletArray = _pool.ToArray();
        }

        public bool TryGetBullet(out Bullet bullet)
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

        private void ReturnBullet(Bullet bullet)
        {
            bullet.gameObject.SetActive(false);
            _pool.Enqueue(bullet);
        }

        public void ReturnBulletByIndex(int index)
        {
            var bullet = _bulletArray[index];
            bullet.gameObject.SetActive(false);
            _pool.Enqueue(bullet);
        }

        private void OnDestroy()
        {
            foreach (var bullet in _pool)
            {
                Destroy(bullet.gameObject);
            }

            _pool.Clear();
            _bulletArray = null;
        }
    }
}