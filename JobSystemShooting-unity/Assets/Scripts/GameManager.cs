using Unity.Collections;
using UnityEngine;
using Shitakami.Boids;
using Shitakami.Boids.SceneObjects;
using Shitakami.RendererUtilities;

namespace Shitakami
{
    [DefaultExecutionOrder(10000)] // MEMO: 最後に実行することで、Jobの完了を待っている間に別の処理が実行できる
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private InstanceDrawer _instanceDrawer;
        [SerializeField] private BoidsSetting _boidsSetting;
        [SerializeField] private BulletPool _bulletPool;
        [SerializeField] private ExplosionEffectPool _explosionEffectPool;

        private BoidsSimulator _avoidObstaclesBoidsSimulator;
        private Transform _transform;

        private bool IsActive => _avoidObstaclesBoidsSimulator != null;

        public void Start()
        {
            _bulletPool.Setup();
            _explosionEffectPool.Setup();

            _transform = transform;

            _avoidObstaclesBoidsSimulator = new BoidsSimulator(
                _boidsSetting,
                _explosionEffectPool.ApplyMovableObstacleToEffect(),
                _bulletPool.GetBulletArray()
            );

            _avoidObstaclesBoidsSimulator.InitializeBoidsPositionAndRotation(_transform.position,
                _transform.localScale);
            _instanceDrawer.Initialize(_boidsSetting.InstanceCount);

            gameObject.SetActive(true);
        }

        private void LateUpdate()
        {
            if (!IsActive)
            {
                return;
            }

            var collisionData = _avoidObstaclesBoidsSimulator.GetCollisionData();
            ApplyCollisionData(collisionData);

            _avoidObstaclesBoidsSimulator.Complete();

            var position = _transform.position;
            var localScale = _transform.localScale;
            _instanceDrawer.SetPositionAndScale(position, localScale);
            _instanceDrawer.Draw(_avoidObstaclesBoidsSimulator.BoidsTransformMatrices);

            _avoidObstaclesBoidsSimulator.ExecuteJob(position, localScale);
        }

        private void ApplyCollisionData(NativeArray<CollisionData> collisionData)
        {
            for (var bulletIndex = 0; bulletIndex < collisionData.Length; bulletIndex++)
            {
                var data = collisionData[bulletIndex];
                if (data.IsCollided)
                {
                    _bulletPool.ReturnBulletByIndex(bulletIndex);
                    if (_explosionEffectPool.TryGetEffect(out var effect))
                    {
                        effect.transform.position = data.Position;
                    }
                }
            }
        }

        private void OnDestroy()
        {
            DisposeAll();
        }

        private void DisposeAll()
        {
            _avoidObstaclesBoidsSimulator?.Dispose();
            _avoidObstaclesBoidsSimulator = null;

            _instanceDrawer.Dispose();
        }
    }
}