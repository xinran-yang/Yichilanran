using UnityEngine;
namespace YiChiLanRan
{
    public sealed class TransferZone2D : MonoBehaviour
    {
        public ZoneSpec data;
        public float Floor => transform.position.y;
        public bool Contains(Rect r) => r.xMin>=data.x-.005f && r.xMax<=data.x+data.width+.005f;
        public bool Accepts(PhaseObject box)
        {
            var support=box.GetComponent<TransferBox2D>();
            float contact=support && support.HasReferenceSupport && support.referenceSupportSide<0 ? box.Bounds.yMax : box.Bounds.yMin;
            return data.green && data.allowBox && Contains(box.Bounds) && Mathf.Abs(contact-Floor)<.055f && Mathf.Abs(box.Body.linearVelocity.y)<.15f;
        }
    }
}

