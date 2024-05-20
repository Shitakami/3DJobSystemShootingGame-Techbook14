using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace Shitakami.RendererUtilities
{
    public class InstanceDrawer : MonoBehaviour, IDisposable
    {
        [SerializeField] private Material _material;
        [SerializeField] private Mesh _mesh;
        [SerializeField] private bool _receiveShadows;
        [SerializeField] private ShadowCastingMode _shadowCastingMode;
        [SerializeField] private LayerMask _layerMask;
        
        private readonly string _matricesArrayPropertyName = "_MatricesArray";

        private GraphicsBuffer _matricesBuffer;
        private GraphicsBuffer _drawArgsBuffer;
        private Bounds _bounds;

        public void Initialize(int instanceCount)
        {
            _matricesBuffer = new GraphicsBuffer(
                GraphicsBuffer.Target.Structured,
                instanceCount,
                Marshal.SizeOf(typeof(Matrix4x4))
            );

            _drawArgsBuffer = new GraphicsBuffer(
                GraphicsBuffer.Target.IndirectArguments,
                5,
                Marshal.SizeOf(typeof(uint)) * 5
            );

            var args = new uint[5];
            args[0] = _mesh.GetIndexCount(0);
            args[1] = (uint) instanceCount;
            args[2] = _mesh.GetIndexStart(0);
            args[3] = _mesh.GetBaseVertex(0);
            args[4] = 0;

            _drawArgsBuffer.SetData(args);

            _material.SetBuffer(_matricesArrayPropertyName, _matricesBuffer);

            var t = transform;
            _bounds = new Bounds(t.position, t.localScale);
        }

        public void SetPositionAndScale(Vector3 position, Vector3 localScale)
        {
            _bounds.center = position;
            _bounds.size = localScale;
        }
        
        public void Draw(NativeArray<Matrix4x4> matricesArray)
        {
            _matricesBuffer.SetData(matricesArray);
            
            Graphics.DrawMeshInstancedIndirect(
                _mesh,
                0,
                _material,
                _bounds,
                _drawArgsBuffer,
                0,
                null,
                _shadowCastingMode,
                _receiveShadows
            );
        }

        public void Dispose()
        {
            _matricesBuffer?.Dispose();
            _drawArgsBuffer?.Dispose();
            _matricesBuffer = null;
            _drawArgsBuffer = null;
        }
    }
}