using System;
using Shitakami.Boids.Data;
using UnityEngine;

namespace Shitakami.Boids.SceneObjects
{
    public class Bullet : MonoBehaviour
    {
        [SerializeField] private float _velocity;
        [SerializeField] private SphereCollider _sphereCollider;
        [SerializeField] private float _lifeTime;
        [SerializeField] private TrailRenderer _trailRenderer;

        private float _timeAlive;

        private Action<Bullet> _onBulletDestroyed;

        private void Update()
        {
            transform.position += transform.forward * _velocity * Time.deltaTime;

            _timeAlive += Time.deltaTime;
            if (_timeAlive >= _lifeTime)
            {
                _onBulletDestroyed?.Invoke(this);
                gameObject.SetActive(false);
            }
        }

        private void OnEnable()
        {
            _timeAlive = 0;
        }

        private void OnDisable()
        {
            _trailRenderer.Clear();
        }

        public void SetBulletDestroyedEvent(Action<Bullet> onBulletDestroyed)
        {
            _onBulletDestroyed = onBulletDestroyed;
        }

        public BulletData GetBulletData()
        {
            if (gameObject.activeInHierarchy)
            {
                return new BulletData
                {
                    Position = transform.position,
                    Velocity = transform.forward * _velocity,
                    Radius = _sphereCollider.radius * transform.lossyScale.x
                };
            }

            return new BulletData();
        }
    }
}