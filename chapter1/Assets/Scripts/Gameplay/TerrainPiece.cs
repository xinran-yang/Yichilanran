using UnityEngine;

namespace YiChiLanRan
{
    // Both complementary colours come from the SAME contour segment.
    public sealed class TerrainPiece : MonoBehaviour
    {
        public bool blue;
        public Vector2 baseCenter;
        public Vector2 size;
        public BoxCollider2D Shape => GetComponent<BoxCollider2D>();
        public Rect WorldRect => new Rect((Vector2)transform.position - size / 2, size);
        public void Apply(bool mirrored, float axis)
        {
            transform.position = new Vector3(baseCenter.x, mirrored ? 2 * axis - baseCenter.y : baseCenter.y, 0);
            // Phase-selective collision is the documented colour rule. The wall is never removed.
            Shape.enabled = blue != mirrored;
            GetComponent<SpriteRenderer>().flipY = mirrored;
        }
    }
}
