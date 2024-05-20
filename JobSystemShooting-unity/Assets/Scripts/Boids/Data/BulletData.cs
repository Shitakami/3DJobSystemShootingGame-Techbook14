using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace Shitakami.Boids.Data
{
    public struct BulletData
    {
        public float3 Position;
        public float3 Velocity;
        public float Radius;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsActive() => Radius != 0;
    }
}