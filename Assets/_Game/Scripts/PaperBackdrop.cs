using UnityEngine;
namespace YiChiLanRan
{
    public sealed class PaperBackdrop : MonoBehaviour
    {
        public Sprite square;
        void Awake()
        {
            var material=GetComponentInChildren<SpriteRenderer>()?.sharedMaterial;
            for(int x=0;x<=24;x++) Line(new Vector2(x,6),new Vector2(.012f,12),material);
            for(int y=0;y<=12;y++) Line(new Vector2(12,y),new Vector2(24,.012f),material);
        }
        void Line(Vector2 center,Vector2 size,Material material)
        {
            var go=new GameObject("Paper grid"); go.transform.SetParent(transform,false); go.transform.position=center;
            var sr=go.AddComponent<SpriteRenderer>(); sr.sprite=square; sr.drawMode=SpriteDrawMode.Sliced; sr.size=size;
            sr.color=new Color(.67f,.76f,.79f,.24f); sr.sortingOrder=-20;
            if(material) sr.sharedMaterial=material;
        }
    }
}
