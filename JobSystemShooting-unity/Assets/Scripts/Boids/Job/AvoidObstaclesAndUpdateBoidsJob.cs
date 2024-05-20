using Shitakami.Boids.Data;
using Shitakami.Boids.Utilities;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Shitakami.Boids.Job
{
    [BurstCompile(FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Low)]
    internal struct AvoidObstaclesAndUpdateBoidsJob : IJobParallelFor
    {
        [ReadOnly] private readonly NativeArray<RaycastHit> _raycastHitsRead;
        [ReadOnly] private readonly NativeArray<bool> _aliveFlagDatasRead;
        [ReadOnly] private readonly float3 _instanceScale;
        [ReadOnly] private readonly float _avoidRotationVelocity;
        [ReadOnly] private readonly float _deltaTime;
        private NativeArray<BoidsData> _boidsDatasWrite;
        [WriteOnly] private NativeArray<Matrix4x4> _boidsTransformMatrices;

        internal AvoidObstaclesAndUpdateBoidsJob(
            NativeArray<RaycastHit> raycastHitsRead,
            NativeArray<bool> aliveFlagDatasRead,
            float3 instanceScale,
            float avoidRotationVelocity,
            float deltaTime,
            NativeArray<BoidsData> boidsDatasWrite,
            NativeArray<Matrix4x4> boidsTransformMatrices
        )
        {
            _raycastHitsRead = raycastHitsRead;
            _aliveFlagDatasRead = aliveFlagDatasRead;
            _instanceScale = instanceScale;
            _avoidRotationVelocity = avoidRotationVelocity;
            _deltaTime = deltaTime;
            _boidsDatasWrite = boidsDatasWrite;
            _boidsTransformMatrices = boidsTransformMatrices;
        }

        public void Execute(int ownIndex)
        {
            if (!_aliveFlagDatasRead[ownIndex])
            {
                _boidsTransformMatrices[ownIndex] = float4x4.zero;
                return;
            }

            var boidsData = _boidsDatasWrite[ownIndex];
            var velocity = boidsData.Velocity;
            var raycastHit = _raycastHitsRead[ownIndex];

            if (raycastHit.IsHit())
            {
                var forward = math.normalize(velocity);
                var axis = math.cross(forward, raycastHit.normal);
                if (math.lengthsq(axis) == 0)
                {
                    axis = new float3(0, 1, 0); // MEMO: 回転軸がない場合はY軸を回転軸とする
                }

                var quaternion =
                    Unity.Mathematics.quaternion.AxisAngle(math.normalize(axis), _avoidRotationVelocity * _deltaTime);
                velocity = math.mul(quaternion, velocity);
            }

            boidsData.Velocity = velocity;
            boidsData.Position += velocity * _deltaTime;

            _boidsDatasWrite[ownIndex] = boidsData;

            var rotationY = math.atan2(boidsData.Velocity.x, boidsData.Velocity.z);
            var rotationX = (float)-math.asin(boidsData.Velocity.y / (math.length(boidsData.Velocity.xyz) + 1e-8));
            var rotation = quaternion.Euler(rotationX, rotationY, 0);
            _boidsTransformMatrices[ownIndex] = float4x4.TRS(boidsData.Position, rotation, _instanceScale);
        }
    }
}