using System.Collections.Generic;
using Shitakami.Boids.Data;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Shitakami.Boids.Job;
using Shitakami.Boids.SceneObjects;

namespace Shitakami.Boids
{
    public class BoidsSimulator
    {
        private readonly BoidsSetting _boidsSetting;
        private readonly int _instanceCount;
        private NativeArray<BoidsData> _boidsDatas;
        private NativeArray<bool> _aliveFlagDatas;
        private NativeParallelMultiHashMap<int3, BoidsDataWithIndex> _gridHashMap;
        private NativeArray<SpherecastCommand> _spherecastCommands;
        private NativeArray<RaycastHit> _raycastHits;
        private NativeArray<float3> _boidsSteers;
        private NativeArray<Matrix4x4> _boidsTransformMatrices;
        private NativeArray<ObstacleData> _obstacleDatas;
        private NativeArray<BulletData> _bulletDatas;
        private NativeArray<CollisionData> _collisionDatas;

        public NativeArray<Matrix4x4> BoidsTransformMatrices => _boidsTransformMatrices;
        private JobHandle _jobHandle;
        private JobHandle _collisionCheckJobHandle;

        public BoidsSimulator(
            BoidsSetting boidsSetting,
            int explosionObstaclesCount,
            int bulletsCount)
        {
            _boidsSetting = boidsSetting;
            _instanceCount = boidsSetting.InstanceCount;

            _boidsDatas = new NativeArray<BoidsData>(_instanceCount, Allocator.Persistent);
            _aliveFlagDatas = new NativeArray<bool>(_instanceCount, Allocator.Persistent);
            _gridHashMap = new NativeParallelMultiHashMap<int3, BoidsDataWithIndex>(_instanceCount, Allocator.Persistent);
            _spherecastCommands = new NativeArray<SpherecastCommand>(_instanceCount, Allocator.Persistent);
            _raycastHits = new NativeArray<RaycastHit>(_instanceCount, Allocator.Persistent);
            _boidsSteers = new NativeArray<float3>(_instanceCount, Allocator.Persistent);
            _boidsTransformMatrices = new NativeArray<Matrix4x4>(_instanceCount, Allocator.Persistent);
            _obstacleDatas = new NativeArray<ObstacleData>(explosionObstaclesCount, Allocator.Persistent);
            _bulletDatas = new NativeArray<BulletData>(bulletsCount, Allocator.Persistent);
            _collisionDatas = new NativeArray<CollisionData>(bulletsCount, Allocator.Persistent);
        }

        public void InitializeBoidsPositionAndRotation(float3 simulationAreaCenter, float3 simulationAreaScale)
        {
            var simulationAreaScaleHalf = simulationAreaScale / 2;
            var initializeVelocity = _boidsSetting.InitializedSpeed;

            for (var i = 0; i < _boidsDatas.Length; ++i)
            {
                _boidsDatas[i] = new BoidsData
                {
                    Position = simulationAreaScaleHalf * UnityEngine.Random.insideUnitSphere + simulationAreaCenter,
                    Velocity = UnityEngine.Random.insideUnitSphere * initializeVelocity
                };

                _aliveFlagDatas[i] = true;
            }
        }

        public void SetExplosionObstacleData(IReadOnlyList<ExplosionObstacle> explosionObstacles)
        {
            for (var i = 0; i < explosionObstacles.Count; i++)
            {
                _obstacleDatas[i] = explosionObstacles[i].ObstacleData;
            }
        }
        
        public void SetBulletData(IReadOnlyList<Bullet> bullets)
        {
            for (var i = 0; i < bullets.Count; i++)
            {
                _bulletDatas[i] = bullets[i].GetBulletData();
            }
        }

        public void ExecuteJob(float3 simulationAreaCenter, float3 simulationAreaScale)
        {
            _gridHashMap.Clear();

            var registerInstanceToGridJob = new RegisterBoidsDataToGridJob
            (
                _gridHashMap.AsParallelWriter(),
                _boidsDatas,
                _aliveFlagDatas,
                _boidsSetting.NeighborSearchGridScale
            );

            var registerInstanceToGridHandle = registerInstanceToGridJob.Schedule(
                _instanceCount,
                _boidsSetting.RegisterInstanceToGridBatchCount);

            var collisionCheckJob = new CollisionCheckJob
            (
                _bulletDatas,
                _gridHashMap,
                _boidsSetting.NeighborSearchGridScale,
                _boidsSetting.CollisionRadius,
                Time.deltaTime,
                _aliveFlagDatas,
                _collisionDatas
            );

            _collisionCheckJobHandle = collisionCheckJob.Schedule(
                _bulletDatas.Length,
                _boidsSetting.CollisionCheckBatchCount,
                registerInstanceToGridHandle);

            var boidsJob = new CalculateBoidsSteerForceByBoidsDatasJob
            (
                _boidsSetting.CohesionWeight,
                _boidsSetting.CohesionAffectedRadiusSqr,
                _boidsSetting.CohesionViewDot,
                _boidsSetting.SeparateWeight,
                _boidsSetting.SeparateAffectedRadiusSqr,
                _boidsSetting.SeparateViewDot,
                _boidsSetting.AlignmentWeight,
                _boidsSetting.AlignmentAffectedRadiusSqr,
                _boidsSetting.AlignmentViewDot,
                _boidsSetting.MaxSpeed,
                _boidsSetting.MaxSteerForce,
                _gridHashMap,
                _boidsSetting.NeighborSearchGridScale,
                _boidsDatas,
                _aliveFlagDatas,
                _boidsSteers
            );

            var boidsJobHandler = boidsJob.Schedule(
                _instanceCount,
                _boidsSetting.CalculateBoidsSteerForceBatchCount,
                JobHandle.CombineDependencies(registerInstanceToGridHandle, _collisionCheckJobHandle));

            var applySteerForce = new ApplySteerForceAndInitializeSphereCommandJob
            (
                _boidsDatas,
                _boidsSteers,
                _obstacleDatas,
                _aliveFlagDatas,
                _spherecastCommands,
                simulationAreaCenter,
                simulationAreaScale / 2,
                _boidsSetting.AvoidSimulationAreaWeight,
                Time.deltaTime,
                _boidsSetting.MaxSpeed,
                _boidsSetting.SpherecastDistance,
                _boidsSetting.SpherecastRadius,
                _boidsSetting.EscapeObstaclesWeight,
                _boidsSetting.EscapeMaxSpeed
            );

            var applySteerForceHandle = applySteerForce.Schedule(
                _instanceCount,
                _boidsSetting.ApplySteerForceWithAvoidanceBatchCount,
                boidsJobHandler);

            var spherecastHandle = SpherecastCommand.ScheduleBatch(
                _spherecastCommands,
                _raycastHits,
                _boidsSetting.SpherecastCommandCommandsPerJob,
                applySteerForceHandle);

            var avoidObstacleAndUpdateBoids = new AvoidObstaclesAndUpdateBoidsJob(
                _raycastHits,
                _aliveFlagDatas,
                _boidsSetting.InstanceScale,
                _boidsSetting.AvoidRotationVelocity,
                Time.deltaTime,
                _boidsDatas,
                _boidsTransformMatrices
            );

            _jobHandle = avoidObstacleAndUpdateBoids.Schedule(
                _instanceCount,
                _boidsSetting.AvoidObstaclesAndUpdateBoidsBatchCount,
                spherecastHandle);

            JobHandle.ScheduleBatchedJobs();
        }

        public void CompleteAllJob()
        {
            _jobHandle.Complete();
        }

        public NativeArray<CollisionData> GetCollisionData()
        {
            _collisionCheckJobHandle.Complete();

            return _collisionDatas;
        }

        public void Dispose()
        {
            // MEMO: Job実行中は NativeArray.Dispose が出来ないので、Jobが完了するまで待つ
            CompleteAllJob();

            _boidsDatas.Dispose();
            _aliveFlagDatas.Dispose();
            _gridHashMap.Dispose();
            _spherecastCommands.Dispose();
            _raycastHits.Dispose();
            _boidsSteers.Dispose();
            _boidsTransformMatrices.Dispose();
            _obstacleDatas.Dispose();
            _bulletDatas.Dispose();
            _collisionDatas.Dispose();
        }
    }
}