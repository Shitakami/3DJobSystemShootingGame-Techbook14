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
    internal struct ApplySteerForceAndInitializeSphereCommandJob : IJobParallelFor
    {
        private NativeArray<BoidsData> _boidsDatasWrite;
        [ReadOnly] private readonly NativeArray<float3> _boidsForceRead;
        [ReadOnly] private readonly NativeArray<ObstacleData> _obstacleDatasRead;
        [ReadOnly] private readonly NativeArray<bool> _aliveFlagDatasRead;
        [WriteOnly] private NativeArray<SpherecastCommand> _spherecastCommandsWrite;

        [ReadOnly] private readonly float3 _simulationAreaCenter;
        [ReadOnly] private readonly float3 _simulationAreaScaleHalf;
        [ReadOnly] private readonly float _avoidWallWeight;

        [ReadOnly] private readonly float _deltaTime;
        [ReadOnly] private readonly float _maxSpeed;
        [ReadOnly] private readonly float _sphereCastDistance;
        [ReadOnly] private readonly float _sphereCastRadius;
        [ReadOnly] private readonly float _escapeObstaclesWeight;
        [ReadOnly] private readonly float _escapeMaxSpeed;

        internal ApplySteerForceAndInitializeSphereCommandJob(
            NativeArray<BoidsData> boidsDatasWrite,
            NativeArray<float3> boidsForceRead,
            NativeArray<ObstacleData> obstacleDatasRead,
            NativeArray<bool> aliveFlagDatasRead,
            NativeArray<SpherecastCommand> spherecastCommandsWrite,
            float3 simulationAreaCenter,
            float3 simulationAreaScaleHalf,
            float avoidWallWeight,
            float deltaTime,
            float maxSpeed,
            float sphereCastDistance,
            float sphereCastRadius,
            float escapeObstaclesWeight,
            float escapeMaxSpeed
        )
        {
            _boidsDatasWrite = boidsDatasWrite;
            _boidsForceRead = boidsForceRead;
            _obstacleDatasRead = obstacleDatasRead;
            _aliveFlagDatasRead = aliveFlagDatasRead;
            _spherecastCommandsWrite = spherecastCommandsWrite;
            _simulationAreaCenter = simulationAreaCenter;
            _simulationAreaScaleHalf = simulationAreaScaleHalf;
            _avoidWallWeight = avoidWallWeight;
            _deltaTime = deltaTime;
            _maxSpeed = maxSpeed;
            _sphereCastDistance = sphereCastDistance;
            _sphereCastRadius = sphereCastRadius;
            _escapeObstaclesWeight = escapeObstaclesWeight;
            _escapeMaxSpeed = escapeMaxSpeed;
        }

        public void Execute(int ownIndex)
        {
            if (!_aliveFlagDatasRead[ownIndex])
            {
                _boidsDatasWrite[ownIndex] = new BoidsData();
                _spherecastCommandsWrite[ownIndex] = new SpherecastCommand();
                return;
            }

            var boidsData = _boidsDatasWrite[ownIndex];
            var force = _boidsForceRead[ownIndex];

            force += MathematicsUtilities.CalculateBoundsForce(boidsData.Position, _simulationAreaCenter,
                _simulationAreaScaleHalf) * _avoidWallWeight;
            var velocity = boidsData.Velocity + (force * _deltaTime);
            boidsData.Velocity = MathematicsUtilities.Limit(velocity, _maxSpeed);

            var escapeForce = float3.zero;

            foreach (var obstacleData in _obstacleDatasRead)
            {
                if (!obstacleData.IsActive())
                {
                    continue;
                }

                var diff = boidsData.Position - obstacleData.Position;
                var distanceSqr = math.lengthsq(diff);
                if (distanceSqr < obstacleData.RadiusSqr)
                {
                    escapeForce += diff / distanceSqr; // 距離の2乗に反比例する力を加える
                }
            }

            escapeForce *= _escapeObstaclesWeight;
            boidsData.Velocity = MathematicsUtilities.Limit(boidsData.Velocity + escapeForce * _deltaTime, _escapeMaxSpeed);

            _boidsDatasWrite[ownIndex] = boidsData;
            _spherecastCommandsWrite[ownIndex] = new SpherecastCommand(
                boidsData.Position,
                _sphereCastRadius,
                math.normalize(boidsData.Velocity),
                QueryParameters.Default,
                _sphereCastDistance);
        }
    }
}