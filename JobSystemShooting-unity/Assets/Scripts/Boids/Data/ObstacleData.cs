using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace Shitakami.Boids.Data
{
    public struct ObstacleData
    {
        public ObstacleData(float3 position, float radius)
        {
            Position = position;
            RadiusSqr = radius * radius;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsActive() => RadiusSqr != 0;

        public float3 Position;
        public float RadiusSqr;
    }
}