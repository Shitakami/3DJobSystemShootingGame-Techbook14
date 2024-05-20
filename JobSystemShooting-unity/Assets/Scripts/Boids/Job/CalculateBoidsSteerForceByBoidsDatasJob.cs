using Shitakami.Boids.Data;
using Shitakami.Boids.Utilities;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Shitakami.Boids.Job
{
    [BurstCompile(FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Low)]
    internal struct CalculateBoidsSteerForceByBoidsDatasJob : IJobParallelFor
    {
        [ReadOnly] private readonly float _cohesionWeight;
        [ReadOnly] private readonly float _cohesionAffectedRadiusSqr;
        [ReadOnly] private readonly float _cohesionViewDot;
        [ReadOnly] private readonly float _separateWeight;
        [ReadOnly] private readonly float _separateAffectedRadiusSqr;
        [ReadOnly] private readonly float _separateViewDot;
        [ReadOnly] private readonly float _alignmentWeight;
        [ReadOnly] private readonly float _alignmentAffectedRadiusSqr;
        [ReadOnly] private readonly float _alignmentViewDot;

        [ReadOnly] private readonly float _maxSpeed;
        [ReadOnly] private readonly float _maxForceSteer;

        [ReadOnly] private NativeParallelMultiHashMap<int3, BoidsDataWithIndex> _gridHashMap;
        [ReadOnly] private readonly float _gridScale;

        [ReadOnly] private readonly NativeArray<BoidsData> _boidsDatasRead;
        [ReadOnly] private readonly NativeArray<bool> _aliveFlagDatasRead;
        [WriteOnly] private NativeArray<float3> _boidsSteerWrite;

        internal CalculateBoidsSteerForceByBoidsDatasJob(
            float cohesionWeight,
            float cohesionAffectedRadiusSqr,
            float cohesionViewDot,
            float separateWeight,
            float separateAffectedRadiusSqr,
            float separateViewDot,
            float alignmentWeight,
            float alignmentAffectedRadiusSqr,
            float alignmentViewDot,
            float maxSpeed,
            float maxForceSteer,
            NativeParallelMultiHashMap<int3, BoidsDataWithIndex> gridHashMap,
            float gridScale,
            NativeArray<BoidsData> boidsDatasRead,
            NativeArray<bool> aliveFlagDatasRead,
            NativeArray<float3> boidsSteerWrite
        )
        {
            _cohesionWeight = cohesionWeight;
            _cohesionAffectedRadiusSqr = cohesionAffectedRadiusSqr;
            _cohesionViewDot = cohesionViewDot;
            _separateWeight = separateWeight;
            _separateAffectedRadiusSqr = separateAffectedRadiusSqr;
            _separateViewDot = separateViewDot;
            _alignmentWeight = alignmentWeight;
            _alignmentAffectedRadiusSqr = alignmentAffectedRadiusSqr;
            _alignmentViewDot = alignmentViewDot;
            _maxSpeed = maxSpeed;
            _maxForceSteer = maxForceSteer;
            _gridHashMap = gridHashMap;
            _gridScale = gridScale;
            _boidsDatasRead = boidsDatasRead;
            _aliveFlagDatasRead = aliveFlagDatasRead;
            _boidsSteerWrite = boidsSteerWrite;
        }

        public void Execute(int ownIndex)
        {
            if (!_aliveFlagDatasRead[ownIndex])
            {
                _boidsSteerWrite[ownIndex] = float3.zero;
                return;
            }

            var ownPosition = _boidsDatasRead[ownIndex].Position;
            var ownVelocity = _boidsDatasRead[ownIndex].Velocity;
            var ownForward = math.normalize(ownVelocity);

            var cohesionPositionSum = new float3();
            var cohesionTargetCount = 0;

            var separateRepulseSum = new float3();
            var separateTargetCount = 0;

            var alignmentVelocitySum = new float3();
            var alignmentTargetCount = 0;

            var gridIndex = MathematicsUtilities.CalculateGridIndex(ownPosition, _gridScale);

            var minX = gridIndex.x - 1;
            var minY = gridIndex.y - 1;
            var minZ = gridIndex.z - 1;

            var maxX = gridIndex.x + 1;
            var maxY = gridIndex.y + 1;
            var maxZ = gridIndex.z + 1;

            for (int x = minX; x <= maxX; ++x)
            for (int y = minY; y <= maxY; ++y)
            for (int z = minZ; z <= maxZ; ++z)
            {
                var key = new int3(x, y, z);

                for (var success = _gridHashMap.TryGetFirstValue(key, out var targetBoidsData, out var iterator);
                     success;
                     success = _gridHashMap.TryGetNextValue(out targetBoidsData, ref iterator))
                {
                    var targetPosition = targetBoidsData.Position;
                    var toTarget = targetPosition - ownPosition;

                    if (toTarget.Equals(float3.zero))
                    {
                        continue;
                    }

                    var targetVelocity = targetBoidsData.Velocity;

                    var distanceSqr = math.lengthsq(toTarget);
                    var toTargetDirection = math.normalize(toTarget);
                    var dot = math.dot(ownForward, toTargetDirection);

                    if (distanceSqr <= _cohesionAffectedRadiusSqr && dot >= _cohesionViewDot)
                    {
                        cohesionPositionSum += targetPosition;
                        cohesionTargetCount++;
                    }

                    if (distanceSqr <= _separateAffectedRadiusSqr && dot >= _separateViewDot)
                    {
                        separateRepulseSum += -toTargetDirection / math.sqrt(distanceSqr); // 距離に反比例する相手から自分への力
                        separateTargetCount++;
                    }

                    if (distanceSqr <= _alignmentAffectedRadiusSqr && dot >= _alignmentViewDot)
                    {
                        alignmentVelocitySum += targetVelocity;
                        alignmentTargetCount++;
                    }
                }
            }

            var cohesionSteer = new float3();
            if (cohesionTargetCount > 0)
            {
                var cohesionPositionAverage = cohesionPositionSum / cohesionTargetCount;
                var cohesionDirection = cohesionPositionAverage - ownPosition;
                var cohesionVelocity = math.normalize(cohesionDirection) * _maxSpeed;
                cohesionSteer = MathematicsUtilities.Limit(cohesionVelocity - ownVelocity, _maxForceSteer);
            }

            var separateSteer = new float3();
            if (separateTargetCount > 0)
            {
                var separateRepulseAverage = separateRepulseSum / separateTargetCount;
                var separateVelocity = math.normalize(separateRepulseAverage) * _maxSpeed;
                separateSteer = MathematicsUtilities.Limit(separateVelocity - ownVelocity, _maxForceSteer);
            }

            var alignmentSteer = new float3();
            if (alignmentTargetCount > 0)
            {
                var alignmentVelocityAverage = alignmentVelocitySum / alignmentTargetCount;
                var alignmentVelocity = math.normalize(alignmentVelocityAverage) * _maxSpeed;
                alignmentSteer = MathematicsUtilities.Limit(alignmentVelocity - ownVelocity, _maxForceSteer);
            }

            _boidsSteerWrite[ownIndex] =
                cohesionSteer * _cohesionWeight +
                separateSteer * _separateWeight +
                alignmentSteer * _alignmentWeight;
        }
    }
}