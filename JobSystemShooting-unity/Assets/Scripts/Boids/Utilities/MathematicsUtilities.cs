using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Mathematics;

namespace Shitakami.Boids.Utilities
{
    [BurstCompile(FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Low)]
    internal static class MathematicsUtilities
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float3 Limit(float3 vec, float max)
        {
            var length = math.sqrt(math.dot(vec, vec));
            return length > max
                ? vec * (max / length)
                : vec;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int3 CalculateGridIndex(float3 position, float gridScale)
        {
            return (int3)math.floor(position / gridScale);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float3 CalculateBoundsForce(float3 position, float3 simulationAreaCenter, float3 simulationAreaScale)
        {
            var acc = new float3();

            acc.x = position.x < simulationAreaCenter.x - simulationAreaScale.x
                ? acc.x + 1.0f
                : acc.x;

            acc.x = position.x > simulationAreaCenter.x + simulationAreaScale.x
                ? acc.x - 1.0f
                : acc.x;

            acc.y = position.y < simulationAreaCenter.y - simulationAreaScale.y
                ? acc.y + 1.0f
                : acc.y;

            acc.y = position.y > simulationAreaCenter.y + simulationAreaScale.y
                ? acc.y - 1.0f
                : acc.y;

            acc.z = position.z < simulationAreaCenter.z - simulationAreaScale.z
                ? acc.z + 1.0f
                : acc.z;

            acc.z = position.z > simulationAreaCenter.z + simulationAreaScale.z
                ? acc.z - 1.0f
                : acc.z;

            return acc;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static float AngleToDot(float angle)
        {
            return math.cos(angle * math.PI / 360);
        }
    }
}