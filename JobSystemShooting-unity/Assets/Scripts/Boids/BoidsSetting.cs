using Shitakami.Boids.Utilities;
using Unity.Mathematics;
using UnityEngine;

namespace Shitakami.Boids
{
    [CreateAssetMenu(fileName = "AvoidObstaclesBoidsSetting", menuName = "Boids/AvoidObstaclesBoidsSetting")]
    public class BoidsSetting : ScriptableObject
    {
        [Header("個体数")] [SerializeField] private int _instanceCount;

        [Header("結合")] [SerializeField] private float _cohesionWeight;
        [SerializeField] private float _cohesionAffectedRadius;
        [SerializeField, Range(0, 360)] private float _cohesionViewAngle;

        [Header("分離")] [SerializeField] private float _separationWeight;
        [SerializeField] private float _separationAffectedRadius;
        [SerializeField, Range(0, 360)] private float _separationViewAngle;

        [Header("整列")] [SerializeField] private float _alignmentWeight;
        [SerializeField] private float _alignmentAffectedRadius;
        [SerializeField, Range(0, 360)] private float _alignmentViewAngle;

        [Header("シミュレーション空間外から戻る力")] [SerializeField]
        private float _avoidSimulationAreaWeight;

        [Header("個体設定")] [SerializeField] private float3 _instanceScale;
        [SerializeField] private float _maxSpeed;
        [SerializeField] private float _maxSteerForce;
        [SerializeField] private float _initializedSpeed;
        [SerializeField] private float _collisionRadius;

        [Header("回避行動の設定")] [SerializeField] private float _spherecastDistance;
        [SerializeField] private float _spherecastRadius;
        [SerializeField] private float _avoidRotateVelocity;
        [SerializeField] private float _escapeObstaclesWeight;
        [SerializeField] private float _escapeMaxSpeed;

        [Header("InnerLoopBatchCount")] [SerializeField]
        private int _registerInstanceToGridBatchCount;

        [SerializeField] private int _calculateBoidsSteerForceBatchCount;
        [SerializeField] private int _spherecastCommandCommandsPerJob;
        [SerializeField] private int _applySteerForceWithAvoidanceBatchCount;
        [SerializeField] private int _avoidObstaclesAndUpdateBoidsBatchCount;
        [SerializeField] private int _collisionCheckBatchCount;

        public int InstanceCount => _instanceCount;

        public float CohesionWeight => _cohesionWeight;
        public float CohesionAffectedRadiusSqr => _cohesionAffectedRadius * _cohesionAffectedRadius;
        public float CohesionViewDot => MathematicsUtilities.AngleToDot(_cohesionViewAngle);

        public float SeparateWeight => _separationWeight;
        public float SeparateAffectedRadiusSqr => _separationAffectedRadius * _separationAffectedRadius;
        public float SeparateViewDot => MathematicsUtilities.AngleToDot(_separationViewAngle);

        public float AlignmentWeight => _alignmentWeight;
        public float AlignmentAffectedRadiusSqr => _alignmentAffectedRadius * _alignmentAffectedRadius;
        public float AlignmentViewDot => MathematicsUtilities.AngleToDot(_alignmentViewAngle);

        public float AvoidSimulationAreaWeight => _avoidSimulationAreaWeight;

        public float NeighborSearchGridScale => math.max(_cohesionAffectedRadius, math.max(_separationAffectedRadius, _alignmentAffectedRadius));

        public float3 InstanceScale => _instanceScale;
        public float MaxSpeed => _maxSpeed;
        public float MaxSteerForce => _maxSteerForce;
        public float InitializedSpeed => _initializedSpeed;
        public float CollisionRadius => _collisionRadius;

        public float SpherecastDistance => _spherecastDistance;
        public float SpherecastRadius => _spherecastRadius;
        public float AvoidRotationVelocity => _avoidRotateVelocity;
        public float EscapeObstaclesWeight => _escapeObstaclesWeight;
        public float EscapeMaxSpeed => _escapeMaxSpeed;

        public int RegisterInstanceToGridBatchCount => _registerInstanceToGridBatchCount;
        public int CalculateBoidsSteerForceBatchCount => _calculateBoidsSteerForceBatchCount;
        public int SpherecastCommandCommandsPerJob => _spherecastCommandCommandsPerJob;
        public int ApplySteerForceWithAvoidanceBatchCount => _applySteerForceWithAvoidanceBatchCount;
        public int AvoidObstaclesAndUpdateBoidsBatchCount => _avoidObstaclesAndUpdateBoidsBatchCount;
        public int CollisionCheckBatchCount => _collisionCheckBatchCount;
    }
}