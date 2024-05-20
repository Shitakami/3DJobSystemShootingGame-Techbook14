using Shitakami.Boids.Data;
using Shitakami.Boids.Utilities;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using Shitakami.Collision;

namespace Shitakami.Boids.Job
{
    [BurstCompile(FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Low)]
    internal struct CollisionCheckJob : IJobParallelFor
    {
        [ReadOnly] private readonly NativeArray<BulletData> _bulletDatasRead;
        [ReadOnly] private NativeParallelMultiHashMap<int3, BoidsDataWithIndex> _gridHashMap;
        [ReadOnly] private readonly float _gridScale;
        [ReadOnly] private readonly float _boidsRadius;
        [ReadOnly] private readonly float _deltaTime;

        [WriteOnly] private NativeArray<bool> _aliveFlagDatasWrite;
        [WriteOnly] private NativeArray<CollisionData> _collisionPositionDatasWrite;

        internal CollisionCheckJob(
            NativeArray<BulletData> bulletDatasRead,
            NativeParallelMultiHashMap<int3, BoidsDataWithIndex> gridHashMap,
            float gridScale,
            float boidsRadius,
            float deltaTime,
            NativeArray<bool> aliveFlagDatasWrite,
            NativeArray<CollisionData> collisionPositionDatasWrite
        )
        {
            _bulletDatasRead = bulletDatasRead;
            _gridHashMap = gridHashMap;
            _gridScale = gridScale;
            _boidsRadius = boidsRadius;
            _deltaTime = deltaTime;
            _aliveFlagDatasWrite = aliveFlagDatasWrite;
            _collisionPositionDatasWrite = collisionPositionDatasWrite;
        }

        public void Execute(int bulletIndex)
        {
            var bulletData = _bulletDatasRead[bulletIndex];

            // MEMO: 弾が存在しない場合は何もしない
            if (!bulletData.IsActive())
            {
                _collisionPositionDatasWrite[bulletIndex] = new CollisionData
                {
                    Position = float3.zero,
                    IsCollided = false
                };
                return;
            }

            var radiusSum = _boidsRadius + bulletData.Radius;

            var gridSearchLength = (int)math.ceil(radiusSum / _gridScale);
            var bulletDirection = math.normalizesafe(bulletData.Velocity);
            var diff = bulletData.Velocity * _deltaTime;
            var moveLengthMax = math.length(diff);

            var minCollisionTime = float.MaxValue;
            var minCollisionPosition = float3.zero;
            var minCollisionBoidsIndex = -1;

            var currentMoveLength = 0f;
            // MEMO: 1フレーム前の探索位置を設定して、初回は周囲の格子全てを探索させる
            var previousSearchGridIndex = MathematicsUtilities.CalculateGridIndex(bulletData.Position - diff, _gridScale);

            while (true)
            {
                var currentSearchPosition = bulletData.Position + bulletDirection * currentMoveLength;
                var currentSearchGridIndex = MathematicsUtilities.CalculateGridIndex(currentSearchPosition, _gridScale);

                var previousMinGridIndex = previousSearchGridIndex - new float3(-gridSearchLength);
                var previousMaxGridIndex = previousSearchGridIndex + new float3(gridSearchLength);

                for (var x = -gridSearchLength; x <= gridSearchLength; x++)
                for (var y = -gridSearchLength; y <= gridSearchLength; y++)
                for (var z = -gridSearchLength; z <= gridSearchLength; z++)
                {
                    var searchGridIndex = currentSearchGridIndex + new int3(x, y, z);

                    // MEMO: すでに探索済みのグリッドはスキップする
                    if (previousMinGridIndex.x <= searchGridIndex.x && searchGridIndex.x <= previousMaxGridIndex.x &&
                        previousMinGridIndex.y <= searchGridIndex.y && searchGridIndex.y <= previousMaxGridIndex.y &&
                        previousMinGridIndex.z <= searchGridIndex.z && searchGridIndex.z <= previousMaxGridIndex.z)
                    {
                        continue;
                    }

                    for (var success =
                             _gridHashMap.TryGetFirstValue(searchGridIndex, out var targetBoidsData, out var iterator);
                         success;
                         success = _gridHashMap.TryGetNextValue(out targetBoidsData, ref iterator))
                    {
                        // MEMO: 破棄された個体は _gridHashMap に登録されないため、生存フラグのチェックは不要

                        var hasCollision = CollisionUtilities.TryDetectStaticAndDynamicCollision(
                            targetBoidsData.Position, _boidsRadius,
                            bulletData.Position, bulletData.Velocity, bulletData.Radius,
                            out var collisionTime, out var collisionPosition
                        );

                        if (hasCollision && collisionTime < minCollisionTime)
                        {
                            minCollisionTime = collisionTime;
                            minCollisionPosition = collisionPosition;
                            minCollisionBoidsIndex = targetBoidsData.Index;
                        }
                    }
                }

                // MEMO: 衝突があった場合は、探索を終了する
                if (minCollisionBoidsIndex != -1)
                {
                    break;
                }

                // MEMO: 次の探索位置を計算する
                var nextSearchGridIndex = currentSearchGridIndex;
                while (math.abs(currentMoveLength - moveLengthMax) < 0.001f)
                {
                    currentMoveLength += radiusSum;
                    currentMoveLength = math.min(currentMoveLength, moveLengthMax);

                    // MEMO: 次の探索位置が同じ格子の場合は、再度距離を加算する
                    var nextSearchPosition = bulletData.Position + bulletDirection * currentMoveLength;
                    nextSearchGridIndex = MathematicsUtilities.CalculateGridIndex(nextSearchPosition, _gridScale);
                    if (!nextSearchGridIndex.Equals(currentSearchGridIndex)) break;
                }

                // MEMO: 次の探索位置が今回と同じ場合、探索を終了する
                if (nextSearchGridIndex.Equals(currentSearchGridIndex))
                {
                    break;
                }

                previousSearchGridIndex = MathematicsUtilities.CalculateGridIndex(currentSearchGridIndex, _gridScale);
            }

            // MEMO: 衝突がない場合は何もしない
            if (minCollisionBoidsIndex == -1)
            {
                _collisionPositionDatasWrite[bulletIndex] = new CollisionData
                {
                    Position = float3.zero,
                    IsCollided = false
                };
                return;
            }

            // MEMO: NativeArrayのアクセス制限回避のため、ポインタを使用して生存状態を書き換える
            unsafe
            {
                var aliveFlagDataWritePtr = (bool*)_aliveFlagDatasWrite.GetUnsafePtr();
                aliveFlagDataWritePtr[minCollisionBoidsIndex] = false;
            }

            _collisionPositionDatasWrite[bulletIndex] = new CollisionData
            {
                Position = minCollisionPosition,
                IsCollided = true
            };
        }
    }
}