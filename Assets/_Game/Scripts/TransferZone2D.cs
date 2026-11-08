using UnityEngine;
namespace YiChiLanRan
{
    public sealed class TransferZone2D : MonoBehaviour
    {
        public ZoneSpec data;
        public float Floor => transform.position.y;
        public Vector2 PositionFor(bool mirrored)
        {
            var world=GetComponentInParent<LevelRuntime>();
            float y=mirrored ? (data.hasMirroredFloor ? data.mirroredFloor : 2*world.definition.mirrorAxis-data.floor) : data.floor;
            return new Vector2(data.x+data.width/2,y);
        }
        public bool Contains(Rect r) => r.xMin>=data.x-.005f && r.xMax<=data.x+data.width+.005f;
        public bool Accepts(PhaseObject box)
        {
            if(!box || box.kind!=ObjectKind.Box || !box.Body) return false;
            // Prefer the actual same-phase floor contact. A remembered underside anchor
            // preserves mirror support, but is not the floor the box now rests upon.
            var bounds=box.Bounds;
            var world=GetComponentInParent<LevelRuntime>();
            var anchor=box.GetComponent<TransferBox2D>();
            bool floorSupport=world && world.Support(box)!=null;
            bool ceilingSupport=!floorSupport && anchor && anchor.HasReferenceSupport && anchor.referenceSupportSide<0;
            float contact=ceilingSupport ? bounds.yMax : bounds.yMin;
            return data.green && data.allowBox && Contains(bounds) && (floorSupport || ceilingSupport) && Mathf.Abs(contact-Floor)<.055f && Mathf.Abs(box.Body.linearVelocity.y)<.15f;
        }
    }
}

