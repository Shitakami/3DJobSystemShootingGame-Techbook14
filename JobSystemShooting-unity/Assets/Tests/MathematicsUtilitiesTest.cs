using NUnit.Framework;
using Unity.Mathematics;
using System.Collections.Generic;
using Shitakami.Collision;

[TestFixture]
public class CollisionUtilitiesTests
{
    private static IEnumerable<CollisionTestCase> CollisionTestCases()
    {
        // 衝突が確実に起こるケース
        yield return new CollisionTestCase(
            new float3(0, 0, 0), new float3(1, 0, 0), 1f,
            new float3(3, 0, 0), new float3(-1, 0, 0), 1f,
            true, new float3(1.5f, 0, 0));

        // 衝突しないケース（平行に動く）
        yield return new CollisionTestCase(
            new float3(0, 0, 0), new float3(0, 1, 0), 1f,
            new float3(3, 0, 0), new float3(0, -1, 0), 1f,
            false, float3.zero);

        // 半径が異なり、衝突するケース
        yield return new CollisionTestCase(
            new float3(0, 0, 0), new float3(0.5f, 0.5f, 0), 0.5f,
            new float3(2, 2, 0), new float3(-0.5f, -0.5f, 0), 0.75f,
            false, float3.zero);

        // 同じ場所から同じ速度で動くが、半径が小さすぎるため衝突しないケース
        yield return new CollisionTestCase(
            new float3(0, 0, 0), new float3(1, 1, 0), 0.1f,
            new float3(0, 0, 0), new float3(1, 1, 0), 0.1f,
            true, float3.zero);

        // 衝突するが、速度ベクトルが反対方向のケース
        yield return new CollisionTestCase(
            new float3(1, 1, 0), new float3(-2, 0, 0), 1f,
            new float3(-1, 1, 0), new float3(2, 0, 0), 1f,
            true, new float3(0, 1, 0));

        // 速度ベクトルが異なり、時間経過後に衝突するケース
        yield return new CollisionTestCase(
            new float3(0, 0, 0), new float3(2, 0, 0), 0.5f,
            new float3(5, 1, 0), new float3(-2, -1, 0), 0.5f,
            true, new float3(2.5f, 0, 0));

        // 完全に逆方向に動いていて衝突しないケース
        yield return new CollisionTestCase(
            new float3(0, 0, 0), new float3(1, 0, 0), 1f,
            new float3(5, 0, 0), new float3(1, 0, 0), 1f,
            false, float3.zero);
        
        // 移動の過程で衝突するケース
        yield return new CollisionTestCase(
            new float3(-2, 0, 0), new float3(4, 0, 0), 1f,
            new float3(0, 2, 0), new float3(0, -4, 0), 1f,
            true, new float3(-0.7071068f, 0.7071068f, 0f));

        yield return new CollisionTestCase(
            new float3(-2, 2, 0), new float3(4, 0, 0), 1f,
            new float3(2, 0, 0), new float3(-4, 0, 0), 1f,
            true, new float3(0, 1, 0f));
    }


    [TestCaseSource(nameof(CollisionTestCases))]
    public void TestCollision(CollisionTestCase testCase)
    {
        var result = CollisionUtilities.TryDetectCollision(
            testCase.aPosition, testCase.aVelocity, testCase.aRadius,
            testCase.bPosition, testCase.bVelocity, testCase.bRadius, 
            out var collisionPosition);

        Assert.AreEqual(testCase.expectedCollision, result, $"Collision detection failed, expected: {testCase.expectedCollision} but was {result}");

        if (result)
        {
            // MEMO: このテストケースでは、衝突位置の誤差は 0.0001 まで許容する
            var error = math.length(collisionPosition - testCase.expectedCollisionPosition);
            Assert.LessOrEqual(error, 0.0001f, $"Collision position failed, expected: {testCase.expectedCollisionPosition} but was {collisionPosition}");
        }
    }
    
    public class CollisionTestCase
    {
        public float3 aPosition, aVelocity;
        public float aRadius;
        public float3 bPosition, bVelocity;
        public float bRadius;
        public bool expectedCollision;
        public float3 expectedCollisionPosition;

        public CollisionTestCase(
            float3 aPosition, float3 aVelocity, float aRadius,
            float3 bPosition, float3 bVelocity, float bRadius,
            bool expectedCollision, float3 expectedCollisionPosition)
        {
            this.aPosition = aPosition;
            this.aVelocity = aVelocity;
            this.aRadius = aRadius;
            this.bPosition = bPosition;
            this.bVelocity = bVelocity;
            this.bRadius = bRadius;
            this.expectedCollision = expectedCollision;
            this.expectedCollisionPosition = expectedCollisionPosition;
        }
    }
}