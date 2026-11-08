using UnityEngine;

namespace YiChiLanRan
{
    public sealed class CrossingPoint : MonoBehaviour
    {
        [Tooltip("Base world coordinates, NOT the mirror axis")]
        public float left, right, baseSurface;
        public float Surface(bool mirrored, float axis) => mirrored ? 2 * axis - baseSurface : baseSurface;
        public bool Contains(float centerX, float halfWidth) => centerX - halfWidth >= left && centerX + halfWidth <= right;
        public void Apply(bool mirrored, float axis)
        {
            transform.position = new Vector3((left + right) / 2, Surface(mirrored, axis), 0);
            // Lotus stays upright; trigger plane is exactly the contour boundary.
        }
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(new Vector3(left, transform.position.y), new Vector3(right, transform.position.y));
        }
    }
}
