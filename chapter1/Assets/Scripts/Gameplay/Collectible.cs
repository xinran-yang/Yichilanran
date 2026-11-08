using UnityEngine;

namespace YiChiLanRan
{
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class Collectible : MonoBehaviour
    {
        public string persistentId;
        public Vector2 baseCenter;
        public Transform visual;
        private bool animating;
        private float feedbackTime;
        public bool Visible => visual && visual.gameObject.activeSelf;
        public void Apply(bool mirrored,float axis,bool collected)
        {
            transform.position=mirrored?ChapterWorld.Reflect(baseCenter,axis):baseCenter;
            animating=false;
            if(visual){visual.localPosition=Vector3.zero;visual.localScale=Vector3.one;visual.gameObject.SetActive(!collected);}
            GetComponent<BoxCollider2D>().enabled=!collected;
        }
        public void TryPickup(ClothPlayer actor)
        {
            if(!actor || actor.ControlsLocked || actor.world.HasCollected(persistentId))return;
            var p=actor.world.Parameters;
            Vector2 center=actor.Foot+Vector2.up*p.PlayerColliderSize.y/2;
            var body=new Rect(center-p.PlayerColliderSize/2,p.PlayerColliderSize);
            var box=GetComponent<BoxCollider2D>();
            var area=new Rect((Vector2)transform.position+box.offset-box.size/2,box.size);
            if(!body.Overlaps(area) || !actor.world.RecordCollection(persistentId))return;
            GetComponent<BoxCollider2D>().enabled=false;
            animating=true;feedbackTime=0;
        }
        private void Update()
        {
            if(!animating || !visual)return;
            feedbackTime+=Time.unscaledDeltaTime;
            float t=Mathf.Clamp01(feedbackTime/.18f);
            visual.localPosition=Vector3.up*(.1f*t);visual.localScale=Vector3.one*Mathf.Lerp(1,.35f,t);
            if(t>=1){animating=false;visual.gameObject.SetActive(false);}
        }
    }
}
