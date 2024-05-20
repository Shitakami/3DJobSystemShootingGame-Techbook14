using Shitakami.Boids.SceneObjects;
using UnityEngine;

namespace Shitakami
{
    public class Shooter : MonoBehaviour
    {
        [SerializeField] private BulletPool _bulletPool;
        [SerializeField] private float _intervalSeconds;
        private float _timeSinceLastShot = 0;

        private void Update()
        {
            if (Input.GetKey(KeyCode.Space))
            {
                if (_timeSinceLastShot >= _intervalSeconds)
                {
                    _timeSinceLastShot = 0;
                    if (_bulletPool.TryGetBullet(out var bullet))
                    {
                        bullet.transform.position = transform.position;
                        bullet.transform.rotation = transform.rotation;
                    }
                }
            }

            _timeSinceLastShot += Time.deltaTime;
        }
    }
}