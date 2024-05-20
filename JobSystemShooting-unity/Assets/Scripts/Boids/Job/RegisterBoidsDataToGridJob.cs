using Shitakami.Boids.Data;
using Shitakami.Boids.Utilities;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Shitakami.Boids.Job
{
    [BurstCompile(FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Low)]
    internal struct RegisterBoidsDataToGridJob : IJobParallelFor
    {
        [WriteOnly] private NativeParallelMultiHashMap<int3, BoidsDataWithIndex>.ParallelWriter _gridWriter;
        [ReadOnly] private readonly NativeArray<BoidsData> _boidsDatasRead;
        [ReadOnly] private readonly NativeArray<bool> _aliveFlagDatasRead;
        [ReadOnly] private readonly float _gridScale;

        internal RegisterBoidsDataToGridJob(
            NativeParallelMultiHashMap<int3, BoidsDataWithIndex>.ParallelWriter gridWriter,
            NativeArray<BoidsData> boidsDatasRead,
            NativeArray<bool> aliveFlagDatasRead,
            float gridScale)
        {
            _gridWriter = gridWriter;
            _boidsDatasRead = boidsDatasRead;
            _aliveFlagDatasRead = aliveFlagDatasRead;
            _gridScale = gridScale;
        }

        public void Execute(int index)
        {
            if (!_aliveFlagDatasRead[index])
            {
                return;
            }

            var boidsDataPosition = _boidsDatasRead[index].Position;

            var gridIndex = MathematicsUtilities.CalculateGridIndex(boidsDataPosition, _gridScale);

            _gridWriter.Add(
                gridIndex,
                new BoidsDataWithIndex
                {
                    Position = _boidsDatasRead[index].Position,
                    Velocity = _boidsDatasRead[index].Velocity,
                    Index = index
                }
            );
        }
    }
}