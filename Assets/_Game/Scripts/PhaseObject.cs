using UnityEngine;
namespace YiChiLanRan
{
    [RequireComponent(typeof(BoxCollider2D),typeof(SpriteRenderer))]
    public sealed class PhaseObject : MonoBehaviour
    {
        public string objectId;
        public Phase phase;
        public ObjectKind kind;
        public bool elastic, breakable;
        public BoxCollider2D Shape => GetComponent<BoxCollider2D>();
        public Rigidbody2D Body => GetComponent<Rigidbody2D>();
        public Vector2 Position => Body ? Body.position : (Vector2)transform.position;
        public Vector2 WorldSize
        {
            get
            {
                var scale=transform.lossyScale;
                return Vector2.Scale(Shape.size+Vector2.one*(2*Shape.edgeRadius),new Vector2(Mathf.Abs(scale.x),Mathf.Abs(scale.y)));
            }
        }
        public Vector2 WorldOffset(Vector2 localOffset)
        {
            var scale=transform.lossyScale;
            return Quaternion.Euler(0,0,Body ? Body.rotation : transform.eulerAngles.z)*new Vector3(localOffset.x*scale.x,localOffset.y*scale.y,0);
        }
        public Rect Bounds => Rules.RectAt(Position+WorldOffset(Shape.offset),WorldSize);
        public void SetPhase(Phase value)
        {
            phase=value;
            if(kind==ObjectKind.Box) elastic=value==Phase.Blue;
            Color ink=new Color(.035f,.23f,.38f),paper=new Color(.98f,.98f,.96f);
            bool actor=kind==ObjectKind.Box || kind==ObjectKind.Player;
            var color=value==Phase.White ? paper : ink;
            if(kind==ObjectKind.BoxBarrier) color=ink;
            GetComponent<SpriteRenderer>().color=actor ? (value==Phase.White ? ink:paper) : color;
            var rim=transform.Find("Inset");
            if(rim) rim.GetComponent<SpriteRenderer>().color=value==Phase.White ? paper:ink;
            foreach(var text in GetComponentsInChildren<TextMesh>()) text.color=value==Phase.White ? ink:paper;
        }
        public void Place(Vector2 p)
        {
            transform.position=new Vector3(p.x,p.y,0);
            if(Body) { Body.position=p; Body.linearVelocity=Vector2.zero; Body.angularVelocity=0; }
        }
    }
}




