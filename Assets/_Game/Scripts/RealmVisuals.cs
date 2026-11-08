using UnityEngine;
namespace YiChiLanRan
{
    public sealed class RealmVisuals : MonoBehaviour
    {
        public LevelRuntime world;
        public Sprite square;
        GameObject ink;
        void Awake()
        {
            ink=new GameObject("White realm ink space"); ink.transform.SetParent(transform,false);
            var fill=ink.AddComponent<SpriteRenderer>(); fill.sprite=square; fill.drawMode=SpriteDrawMode.Sliced;
            fill.size=new Vector2(23.8f,11.8f); ink.transform.position=new Vector3(12,6,0); fill.color=new Color(.035f,.23f,.38f); fill.sortingOrder=-19;
            var sample=world.GetComponentInChildren<PhaseObject>(); if(sample) fill.sharedMaterial=sample.GetComponent<SpriteRenderer>().sharedMaterial;
            // Decoration only: filled WHITE colliders above this layer define exactly which areas stay blue.
            for(float x=.8f;x<24;x+=1.7f) for(float y=.7f;y<12;y+=1.7f)
                for(int petal=0;petal<4;petal++)
                {
                    var mark=new GameObject("Ink weave"); mark.transform.SetParent(ink.transform,false);
                    float a=petal*Mathf.PI/2; mark.transform.position=new Vector3(x+Mathf.Cos(a)*.1f,y+Mathf.Sin(a)*.1f,0);
                    mark.transform.rotation=Quaternion.Euler(0,0,45);
                    var sr=mark.AddComponent<SpriteRenderer>(); sr.sprite=square; sr.drawMode=SpriteDrawMode.Sliced; sr.size=new Vector2(.1f,.1f);
                    sr.color=new Color(.8f,.91f,.94f,.17f); sr.sortingOrder=-18; sr.sharedMaterial=fill.sharedMaterial;
                }
            Apply();
        }
        public void Apply()
        {
            if(!world) return;
            if(ink) ink.SetActive(world.Mirrored);
            foreach(var o in world.Objects)
                if(o.kind==ObjectKind.Terrain && o.phase==Phase.White)
                    foreach(var sr in o.GetComponentsInChildren<SpriteRenderer>(true)) sr.enabled=world.Mirrored;
        }
    }
}
