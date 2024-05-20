using Unity.Burst;
using Unity.Mathematics;

namespace Shitakami.Collision
{
    [BurstCompile(FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Low)]
    public static class CollisionUtilities
    {
        public static bool TryDetectionCollision(
            float3 aPosition, float aRadius,
            float3 bPosition, float bRadius
        )
        {
            var radiusSum = aRadius + bRadius;
            var relativePosition = bPosition - aPosition;
            return math.dot(relativePosition, relativePosition) <= radiusSum * radiusSum;
        }

        public static bool TryDetectCollision(
            float3 aPosition, float3 aVelocity, float aRadius,
            float3 bPosition, float3 bVelocity, float bRadius,
            out float3 collisionPosition
        )
        {
            collisionPosition = float3.zero;

            var radiusSum = aRadius + bRadius;

            var relativePosition = bPosition - aPosition;
            var relativeVelocity = bVelocity - aVelocity;

            var a = math.dot(relativeVelocity, relativeVelocity);

            // MEMO: 球体同士が平行移動している場合
            if (a < float.Epsilon)
            {
                if (math.dot(relativePosition, relativePosition) <= radiusSum * radiusSum)
                {
                    collisionPosition = aPosition + relativePosition / 2f;
                    return true;
                }

                return false;
            }

            var b = math.dot(relativePosition, relativeVelocity);
            var c = math.dot(relativePosition, relativePosition) - radiusSum * radiusSum;

            var discriminant = b * b - a * c;

            // MEMO: 解なし
            if (discriminant < 0)
            {
                return false;
            }

            var discriminantSqrt = math.sqrt(discriminant);

            // MEMO: 最初の衝突のみを求める
            var t1 = (-b - discriminantSqrt) / a;
            if (0 <= t1 && t1 <= 1)
            {
                collisionPosition = aPosition + aVelocity * t1 +
                                    (relativePosition + relativeVelocity * t1) * aRadius / radiusSum;
                return true;
            }

            return false;
        }

        public static bool TryDetectStaticAndDynamicCollision(
            float3 staticPosition, float staticRadius,
            float3 dynamicPosition, float3 dynamicVelocity, float dynamicRadius,
            out float collisionTime, out float3 collisionPosition
        )
        {
            collisionPosition = float3.zero;
            collisionTime = 0f;

            var radiusSum = staticRadius + dynamicRadius;

            var relativePosition = dynamicPosition - staticPosition;

            var a = math.dot(dynamicVelocity, dynamicVelocity);

            // MEMO: 球体同士が平行移動している場合は、球体同士が重なっているかで判別する
            if (a < float.Epsilon)
            {
                if (math.dot(relativePosition, relativePosition) <= radiusSum * radiusSum)
                {
                    collisionPosition = staticPosition + relativePosition / 2f;
                    return true;
                }

                return false;
            }

            var b = math.dot(relativePosition, dynamicVelocity);
            var c = math.dot(relativePosition, relativePosition) - radiusSum * radiusSum;

            var discriminant = b * b - a * c;

            // MEMO: 解なし
            if (discriminant < 0)
            {
                return false;
            }

            var discriminantSqrt = math.sqrt(discriminant);

            // MEMO: 最初の衝突のみを求める
            var t1 = (-b - discriminantSqrt) / a;
            if (0 <= t1 && t1 <= 1)
            {
                collisionTime = t1;
                collisionPosition =
                    staticPosition + (relativePosition + dynamicVelocity * t1) * staticRadius / radiusSum;
                return true;
            }

            return false;
        }
    }
}