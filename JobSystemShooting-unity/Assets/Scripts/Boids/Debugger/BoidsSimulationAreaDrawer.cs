using UnityEngine;

namespace Shitakami.Boids.Debugger
{
    public class BoidsSimulationAreaDrawer : MonoBehaviour
    {
        [SerializeField] private Color _gizmoColor = Color.yellow;
        [SerializeField] private bool _drawArea = true;
        private void OnDrawGizmos()
        {
            if (!_drawArea) return;
            
            Gizmos.color = _gizmoColor;
            Gizmos.matrix = Matrix4x4.TRS(transform.position, Quaternion.identity, transform.localScale);
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        }
    }
}
