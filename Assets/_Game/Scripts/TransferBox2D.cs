using UnityEngine;
namespace YiChiLanRan
{
    public sealed class TransferBox2D : MonoBehaviour
    {
        public bool participateInPhysics=true;
        public string referenceSupportId="";
        public int referenceSupportSide=1;
        [System.NonSerialized] public float inspectionPush;
        public PhaseObject Entity => GetComponent<PhaseObject>();
        public bool ShouldSimulate(LevelRuntime world) => participateInPhysics && Entity.phase==world.Player.Entity.phase;
        public bool HasReferenceSupport => !string.IsNullOrEmpty(referenceSupportId);
        public float CustomGravityY(LevelRuntime world) => ShouldSimulate(world) && !HasReferenceSupport ? -Rules.Gravity : 0;
        public void ApplyPhaseState(LevelRuntime world,bool allowSimulation=true)
        {
            var body=Entity.Body;
            body.constraints=RigidbodyConstraints2D.FreezeRotation|RigidbodyConstraints2D.FreezePositionX|(HasReferenceSupport ? RigidbodyConstraints2D.FreezePositionY : RigidbodyConstraints2D.None);
            body.simulated=allowSimulation && ShouldSimulate(world);
            if(!body.simulated || HasReferenceSupport) { body.linearVelocity=Vector2.zero; body.angularVelocity=0; }
        }
        public void CaptureReferenceSupport(LevelRuntime world)
        {
            var r=Entity.Bounds;
            if(HasReferenceSupport)
            {
                var support=world.Objects.Find(o=>o.objectId==referenceSupportId);
                if(support && support.gameObject.activeInHierarchy)
                {
                    var b=support.Bounds;
                    float gap=referenceSupportSide>0 ? r.yMin-b.yMax : b.yMin-r.yMax;
                    if(r.xMax>b.xMin+.025f && r.xMin<b.xMax-.025f && Mathf.Abs(gap)<.065f) return;
                }
                referenceSupportId=""; referenceSupportSide=1;
            }
            // Acquire actual touching support only. Never search for or teleport onto a distant platform.
            if(Mathf.Abs(Entity.Body.linearVelocity.y)>.2f) return;
            foreach(var support in world.Objects)
            {
                if(support==Entity || (support.kind!=ObjectKind.Terrain && support.kind!=ObjectKind.Box) || !support.gameObject.activeInHierarchy || (support.kind==ObjectKind.Box && support.phase!=Entity.phase)) continue;
                var b=support.Bounds; float gap=r.yMin-b.yMax;
                if(r.xMax>b.xMin+.025f && r.xMin<b.xMax-.025f && gap>=-.01f && gap<=.035f)
                { referenceSupportId=support.objectId; referenceSupportSide=1; return; }
            }
        }
        public void Tick(LevelRuntime world,float dt)
        {
            var box=Entity; var body=box.Body;
            CaptureReferenceSupport(world);
            if(!ShouldSimulate(world))
            {
                if(body.simulated) ApplyPhaseState(world);
                body.linearVelocity=Vector2.zero; body.angularVelocity=0; return;
            }
            var p=world.Player; var r=box.Bounds; var pr=p.Entity.Bounds;
            bool side=pr.yMax>r.yMin+.12f && pr.yMin<r.yMax-.12f;
            bool push=p.Grounded && side && ((p.Axis>0 && Mathf.Abs(pr.xMax-r.xMin)<.11f) || (p.Axis<0 && Mathf.Abs(pr.xMin-r.xMax)<.11f));
            float pushAxis=p.Axis;
            if(world.definition.mirrorInspection && Mathf.Abs(inspectionPush)>.01f) { push=true; pushAxis=inspectionPush; }
            if(push && world.Support(box)!=null) world.TrySmallStep(box,p.Axis,dt);
            body.constraints=RigidbodyConstraints2D.FreezeRotation|(push ? RigidbodyConstraints2D.None : RigidbodyConstraints2D.FreezePositionX)|(HasReferenceSupport ? RigidbodyConstraints2D.FreezePositionY : RigidbodyConstraints2D.None);
            body.linearVelocity=new Vector2(push ? pushAxis*Rules.Speed : 0,HasReferenceSupport ? 0 : Mathf.Max(-Rules.MaxFall,body.linearVelocity.y-Rules.Gravity*dt));
        }
    }
}


