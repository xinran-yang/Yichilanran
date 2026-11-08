using UnityEngine;

namespace YiChiLanRan
{
    // A non-text flag, using the existing cloth material and flag geometry.
    public sealed class ProgressMarker : MonoBehaviour
    {
        public string checkpointId;
        public Vector2 baseFoot;
        public void Apply(bool mirrored,float axis,bool active)
        {
            Vector2 foot=mirrored?ChapterWorld.Reflect(baseFoot,axis):baseFoot;
            transform.position=foot+Vector2.up*.275f;
            foreach(var sr in GetComponentsInChildren<SpriteRenderer>())
                sr.color=active?(mirrored?ClothArt.Cream:ClothArt.Blue):ClothArt.Pattern;
        }
    }
}
