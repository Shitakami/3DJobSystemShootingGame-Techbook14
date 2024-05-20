using Shitakami.Boids.Data;
using UnityEngine;

namespace Shitakami.Boids.SceneObjects
{
    [RequireComponent(typeof(SphereCollider))]
    public class ExplosionObstacle : MonoBehaviour
    {
        private Transform _transform;
        private SphereCollider _sphereCollider;
        private float _lossyScale;

        private void Awake()
        {
            _transform = transform;
            _sphereCollider = GetComponent<SphereCollider>();
            _lossyScale = _transform.lossyScale.x;
        }

        public ObstacleData ObstacleData
            => gameObject.activeInHierarchy
                ? new ObstacleData(_transform.position, _sphereCollider.radius * _lossyScale)
                : new ObstacleData();
    }
}