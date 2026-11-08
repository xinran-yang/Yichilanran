using System;
using System.Collections.Generic;
using UnityEngine;
namespace YiChiLanRan
{
    public sealed class WorldMirrorSystem
    {
        const float Epsilon=.00001f;
        internal sealed class Pose
        {
            public Vector2 source,target,sourceOffset,targetOffset,size,sourceCenter;
            public Phase phase;
            public string supportId; public int supportSide;
        }
        internal sealed class BodyState
        {
            public Rigidbody2D body;
            public bool simulated;
            public float gravity,angularVelocity;
            public Vector2 velocity;
            public RigidbodyInterpolation2D interpolation;
            public RigidbodyConstraints2D constraints;
        }
        public sealed class Plan
        {
            internal readonly Dictionary<PhaseObject,Pose> poses=new Dictionary<PhaseObject,Pose>();
            internal readonly Dictionary<TransferZone2D,Vector2> zones=new Dictionary<TransferZone2D,Vector2>();
            internal readonly Dictionary<TransferZone2D,Vector2> zoneTargets=new Dictionary<TransferZone2D,Vector2>();
            internal readonly List<BodyState> bodies=new List<BodyState>();
            internal string id=Guid.NewGuid().ToString("N").Substring(0,8);
            internal TransferZone2D zone;
            internal PhaseObject box;
            internal Phase target;
            internal float axis;
            internal Vector2 goal;
            internal Vector3 goalScale;
            internal readonly Dictionary<Transform,Vector3> visualPositions=new Dictionary<Transform,Vector3>();
            internal readonly Dictionary<Transform,Quaternion> visualRotations=new Dictionary<Transform,Quaternion>();
            internal bool committed,oldMirrored;
            internal int oldTransfers,oldEventCount;
            internal string oldEvent;
            public Vector2 TargetOf(PhaseObject o) => poses[o].target;
        }
        readonly LevelRuntime world;
        Plan active;
        public WorldMirrorSystem(LevelRuntime world) { this.world=world; }
        public bool HasPhysicsLock => active!=null;
        public bool TryPlan(TransferZone2D zone,PhaseObject box,out Plan plan,bool inspection=false)
        {
            plan=null;
            if(world.Busy || world.Complete || active!=null) return Reject("Transfer busy");
            if(inspection && !world.definition.mirrorInspection) return Reject("Inspection switch is disabled in gameplay scenes");
            if(!inspection && !world.Player.Diving) return Reject("Dive required");
            if(zone==null || !world.Zones.Contains(zone)) return Reject("Unknown transfer zone");
            var player=world.Player.Entity;
            if(box) box.GetComponent<TransferBox2D>().CaptureReferenceSupport(world);
            if(box!=null && (!world.Objects.Contains(box) || box.kind!=ObjectKind.Box || box.phase!=player.phase || !zone.Accepts(box) || (!box.GetComponent<TransferBox2D>().HasReferenceSupport && world.Support(box)==null)))
                return Reject("Box must be same phase, settled and fully on pad");
            if(box==null && !zone.data.allowSolo) return Reject("This pad requires a box");
            var result=new Plan { zone=zone,box=box,target=Rules.Opposite(player.phase),axis=world.definition.mirrorAxis,
                oldMirrored=world.Mirrored,oldTransfers=world.Transfers,oldEventCount=world.EventHistory.Count,oldEvent=world.LastEvent };
            if(float.IsNaN(result.axis) || float.IsInfinity(result.axis)) return Reject("Invalid mirror axis");
            foreach(var o in world.Objects)
            {
                // All current levels use upright rectangles. Reject unsupported rotations rather than silently mis-map an offset collider.
                float angle=o.Body ? o.Body.rotation : o.transform.eulerAngles.z;
                if(Mathf.Abs(Mathf.DeltaAngle(0,angle))>.001f) return Reject("Mirror requires axis-aligned collider: "+o.objectId);
                var transferBox=o.GetComponent<TransferBox2D>();
                if(transferBox) transferBox.CaptureReferenceSupport(world);
                result.poses[o]=new Pose { source=o.Position,target=Rules.Mirror(o.Position,result.axis),phase=o.phase,
                    sourceOffset=o.Shape.offset,targetOffset=new Vector2(o.Shape.offset.x,-o.Shape.offset.y),size=o.WorldSize,sourceCenter=o.Bounds.center,supportId=transferBox ? transferBox.referenceSupportId : "",supportSide=transferBox ? transferBox.referenceSupportSide : 1 };
            }
            foreach(var z in world.Zones) { result.zones[z]=z.transform.position; result.zoneTargets[z]=z.PositionFor(!world.Mirrored); }
            if(world.Goal) { result.goal=world.Goal.position; result.goalScale=world.Goal.localScale; }
            foreach(var o in world.Objects) if(o.kind==ObjectKind.Terrain || o.kind==ObjectKind.BoxBarrier) foreach(Transform child in o.transform)
            { result.visualPositions[child]=child.localPosition; result.visualRotations[child]=child.localRotation; }
            // Player alone uses the confirmed safe-landing policy. No box target is ever overwritten by a floor search or pad snap.
            var pp=result.poses[player];
            Rect boxTarget=box!=null ? TargetBounds(box,result.poses[box]) : default;
            float bottom=box!=null ? boxTarget.yMax+Rules.Skin : result.zoneTargets[zone].y+Rules.Skin;
            float centerX=box!=null ? boxTarget.center.x : player.Bounds.center.x;
            pp.target=new Vector2(centerX,bottom+pp.size.y/2)-player.WorldOffset(pp.targetOffset);
            if(!TargetsSafe(result,out string reason)) { Audit(result,"REJECTED",false,reason); return Reject(reason); }
            plan=result; return true;
        }
        Rect TargetBounds(PhaseObject o,Pose p) => Rules.RectAt(p.target+o.WorldOffset(p.targetOffset),p.size);
        bool TargetsSafe(Plan plan,out string reason)
        {
            foreach(var pair in plan.poses)
            {
                var actor=pair.Key; if(!actor.Body || !actor.gameObject.activeInHierarchy || !actor.Shape.enabled) continue;
                if(actor.kind==ObjectKind.Box && (pair.Value.phase!=(actor==plan.box ? world.Player.Entity.phase : plan.target) || !actor.GetComponent<TransferBox2D>().participateInPhysics)) continue;
                Rect r=TargetBounds(actor,pair.Value);
                Phase ap=actor==world.Player.Entity || actor==plan.box ? plan.target : pair.Value.phase;
                if(r.xMin<world.definition.bounds.xMin || r.xMax>world.definition.bounds.xMax || r.yMin<world.definition.bounds.yMin || r.yMax>world.definition.bounds.yMax)
                { reason="Unsafe exact mirror: "+actor.objectId+" outside world"; return false; }
                foreach(var other in plan.poses)
                {
                    if(other.Key==actor || !other.Key.gameObject.activeInHierarchy || !other.Key.Shape.enabled) continue;
                    Phase bp=other.Key==world.Player.Entity || other.Key==plan.box ? plan.target : other.Value.phase;
                    if(Rules.Collides(actor.kind,ap,other.Key.kind,bp) && Rules.Overlap(r,TargetBounds(other.Key,other.Value)))
                    { reason="Unsafe exact mirror: "+actor.objectId+" / "+other.Key.objectId; return false; }
                }
            }
            reason=""; return true;
        }
        bool SourceUnchanged(Plan plan)
        {
            if(world.definition.mirrorAxis!=plan.axis || world.Mirrored!=plan.oldMirrored || world.Transfers!=plan.oldTransfers) return false;
            foreach(var pair in plan.poses)
                if(Vector2.Distance(pair.Key.Position,pair.Value.source)>Epsilon || pair.Key.phase!=pair.Value.phase || pair.Key.Shape.offset!=pair.Value.sourceOffset || Vector2.Distance(pair.Key.WorldSize,pair.Value.size)>Epsilon) return false;
            foreach(var z in plan.zones) if(Vector2.Distance(z.Key.transform.position,z.Value)>Epsilon) return false;
            foreach(var z in plan.zoneTargets) if(Vector2.Distance(z.Key.PositionFor(!plan.oldMirrored),z.Value)>Epsilon) return false;
            return !world.Goal || (Vector2.Distance(world.Goal.position,plan.goal)<=Epsilon && world.Goal.localScale==plan.goalScale);
        }
        bool Reject(string reason) { world.Failure=reason; return false; }
        public bool TryTransfer(TransferZone2D zone,PhaseObject box)
        {
            if(!TryPlan(zone,box,out var plan)) { world.Player.EndDive(); return false; }
            return world.Transition(plan);
        }
        public bool TryInspectionTransfer(TransferZone2D zone)
        {
            if(!TryPlan(zone,null,out var plan,true)) return false;
            return world.Transition(plan);
        }
        internal bool Begin(Plan plan)
        {
            if(active!=null || world.Busy || !SourceUnchanged(plan)) return Reject("Stale mirror plan: source changed");
            if(!TargetsSafe(plan,out string reason)) return Reject(reason);
            active=plan; world.Busy=true; world.Player.ClearInput();
            foreach(var o in world.Objects)
            {
                var b=o.Body; if(!b) continue;
                var transferBox=o.GetComponent<TransferBox2D>(); if(transferBox) transferBox.inspectionPush=0;
                plan.bodies.Add(new BodyState { body=b,simulated=b.simulated,gravity=b.gravityScale,velocity=b.linearVelocity,angularVelocity=b.angularVelocity,interpolation=b.interpolation,constraints=b.constraints });
                b.simulated=false; b.interpolation=RigidbodyInterpolation2D.None; b.gravityScale=0;
                b.linearVelocity=Vector2.zero; b.angularVelocity=0;
            }
            Physics2D.SyncTransforms(); return true;
        }
        internal bool Commit(Plan plan)
        {
            if(active!=plan) return Reject("Missing mirror physics lock");
            if(!SourceUnchanged(plan) || !TargetsSafe(plan,out string reason))
            { Abort(plan,"Mirror source or target changed while locked"); return false; }
            foreach(var pair in plan.poses)
            {
                pair.Key.Shape.offset=pair.Value.targetOffset; pair.Key.Place(pair.Value.target);
                var box=pair.Key.GetComponent<TransferBox2D>();
                if(box) { box.referenceSupportId=pair.Value.supportId; box.referenceSupportSide=-pair.Value.supportSide; }
            }
            world.Player.Entity.SetPhase(plan.target);
            if(plan.box!=null) plan.box.SetPhase(plan.target);
            foreach(var z in plan.zoneTargets) z.Key.transform.position=z.Value;
            if(world.Goal) { world.Goal.position=Rules.Mirror(plan.goal,plan.axis); world.Goal.localScale=new Vector3(plan.goalScale.x,-plan.goalScale.y,plan.goalScale.z); }
            foreach(var pair in plan.visualPositions)
            {
                pair.Key.localPosition=new Vector3(pair.Value.x,-pair.Value.y,pair.Value.z);
                pair.Key.localRotation=Quaternion.Euler(0,0,-plan.visualRotations[pair.Key].eulerAngles.z);
            }
            Physics2D.SyncTransforms();
            if(!AppliedCorrectly(plan,out reason)) { Abort(plan,reason); return false; }
            plan.committed=true; world.Mirrored=!plan.oldMirrored; world.Transfers=plan.oldTransfers+1;
            world.ApplyRealmVisuals();
            world.LastEvent=plan.zone.data.id+(plan.box!=null ? " + "+plan.box.objectId : " solo");
            world.EventHistory.Add(world.LastEvent); world.Failure="";
            world.Player.AfterTransfer(world.Clock); world.MirrorCommittedThisTick=true;
            Audit(plan,"COMMIT_FROZEN",true,""); return true;
        }
        bool AppliedCorrectly(Plan plan,out string reason)
        {
            foreach(var pair in plan.poses)
            {
                var o=pair.Key; var p=pair.Value;
                Phase expectedPhase=o==world.Player.Entity || o==plan.box ? plan.target : p.phase;
                if(o.phase!=expectedPhase) { reason="Mirror color mismatch: "+o.objectId; return false; }
                if(Vector2.Distance(o.Position,p.target)>Epsilon || o.Shape.offset!=p.targetOffset)
                { reason="Mirror pose mismatch: "+o.objectId; return false; }
                if(o.kind==ObjectKind.Box && (Vector2.Distance(o.Position,Rules.Mirror(p.source,plan.axis))>Epsilon || Vector2.Distance(o.Bounds.center,Rules.Mirror(p.sourceCenter,plan.axis))>Epsilon))
                { reason="Box mirror formula failed: "+o.objectId; return false; }
            }
            foreach(var actor in world.Objects)
            {
                if(!actor.Body || !actor.gameObject.activeInHierarchy || !actor.Shape.enabled) continue;
                if(actor.kind==ObjectKind.Box && !actor.GetComponent<TransferBox2D>().ShouldSimulate(world)) continue;
                foreach(var other in world.Objects)
                    if(other!=actor && other.gameObject.activeInHierarchy && other.Shape.enabled && PhaseCollisionSystem.Collides(actor,other) && Rules.Overlap(actor.Bounds,other.Bounds))
                    { reason="Post-mirror collision: "+actor.objectId+" / "+other.objectId; return false; }
            }
            reason=""; return true;
        }
        internal bool Finish(Plan plan)
        {
            if(active!=plan || !plan.committed) return false;
            if(!AppliedCorrectly(plan,out string reason)) { Abort(plan,reason); return false; }
            Audit(plan,"RESUME_FROZEN",true,"");
            RestoreBodies(plan,false);
            if(!AppliedCorrectly(plan,out reason)) { Abort(plan,reason); return false; }
            Audit(plan,"RESTORED_BEFORE_STEP",true,""); active=null; world.Busy=false;
            return true;
        }
        void RestoreBodies(Plan plan,bool restoreVelocity)
        {
            foreach(var state in plan.bodies)
            {
                var b=state.body; if(!b) continue;
                b.gravityScale=state.gravity; b.constraints=state.constraints; b.interpolation=state.interpolation;
                b.linearVelocity=restoreVelocity ? state.velocity : Vector2.zero;
                b.angularVelocity=restoreVelocity ? state.angularVelocity : 0;
                var transferBox=b.GetComponent<TransferBox2D>();
                if(transferBox && !restoreVelocity) transferBox.ApplyPhaseState(world);
                else b.simulated=state.simulated;
            }
            // IgnoreCollision pairs must be reapplied AFTER bodies rejoin the simulation.
            Physics2D.SyncTransforms(); PhaseCollisionSystem.Refresh(world);
        }
        void Abort(Plan plan,string reason)
        {
            foreach(var pair in plan.poses) { pair.Key.Shape.offset=pair.Value.sourceOffset; pair.Key.Place(pair.Value.source); pair.Key.SetPhase(pair.Value.phase); }
            foreach(var z in plan.zones) z.Key.transform.position=z.Value;
            if(world.Goal) { world.Goal.position=plan.goal; world.Goal.localScale=plan.goalScale; }
            foreach(var pair in plan.visualPositions) { pair.Key.localPosition=pair.Value; pair.Key.localRotation=plan.visualRotations[pair.Key]; }
            foreach(var pair in plan.poses) { var box=pair.Key.GetComponent<TransferBox2D>(); if(box) { box.referenceSupportId=pair.Value.supportId; box.referenceSupportSide=pair.Value.supportSide; } }
            world.Mirrored=plan.oldMirrored; world.Transfers=plan.oldTransfers; world.LastEvent=plan.oldEvent;
            world.ApplyRealmVisuals();
            while(world.EventHistory.Count>plan.oldEventCount) world.EventHistory.RemoveAt(world.EventHistory.Count-1);
            RestoreBodies(plan,true); active=null; world.Busy=false; world.MirrorCommittedThisTick=false;
            world.Player.EndDive(); Audit(plan,"ROLLED_BACK",false,reason); Reject(reason);
        }
        public void CancelForReset()
        {
            if(active==null) return;
            RestoreBodies(active,false); active=null;
        }
        void Audit(Plan plan,string moment,bool applied,string reason)
        {
            world.LastMirrorAudit.Clear();
            foreach(var pair in plan.poses)
            {
                var o=pair.Key; if(o.kind!=ObjectKind.Box) continue;
                var p=pair.Value;
                var entry=new MirrorBoxAudit { transaction=plan.id,zone=plan.zone.data.id,moment=moment,axis=plan.axis,box=o.objectId,
                    participated=o==plan.box,applied=applied,before=p.source,expected=Rules.Mirror(p.source,plan.axis),after=o.Position,
                    colliderBefore=p.sourceCenter,colliderExpected=Rules.Mirror(p.sourceCenter,plan.axis),colliderAfter=o.Bounds.center,
                    colorBefore=p.phase,colorTarget=o==plan.box ? plan.target : p.phase,colorAfter=o.phase,reason=reason };
                var boxController=o.GetComponent<TransferBox2D>();
                entry.simulated=o.Body.simulated; entry.colliderEnabled=o.Shape.enabled; entry.solidForPlayer=PhaseCollisionSystem.Collides(o,world.Player.Entity);
                entry.gravityScale=o.Body.gravityScale; entry.customGravityY=boxController.CustomGravityY(world);
                entry.supportId=boxController.referenceSupportId; entry.supportSide=boxController.referenceSupportSide;
                entry.positionError=Vector2.Distance(entry.expected,entry.after); entry.colliderError=Vector2.Distance(entry.colliderExpected,entry.colliderAfter);
                entry.formulaVerified=applied && entry.positionError<=Epsilon && entry.colliderError<=Epsilon && entry.colorAfter==entry.colorTarget;
                world.LastMirrorAudit.Add(entry);
                if(world.logMirrorTransactions) Debug.Log("[MirrorAudit] "+entry.box+" color="+entry.colorBefore+"->"+entry.colorAfter+" participating="+entry.participated+" "+JsonUtility.ToJson(entry),world);
            }
        }
        public void DumpLiveState()
        {
            Debug.Log("[MirrorState] axis="+world.definition.mirrorAxis+" mirrored="+world.Mirrored+" player="+world.Player.Entity.phase+" Physics2D.gravity="+Physics2D.gravity,world);
            foreach(var o in world.Objects)
            {
                var box=o.GetComponent<TransferBox2D>();
                if(box) Debug.Log("[MirrorBoxLive] "+o.objectId+" center="+o.Position.ToString("F6")+" phase="+o.phase+" simulated="+o.Body.simulated+" gravityScale="+o.Body.gravityScale+" customGravityY="+box.CustomGravityY(world)+" solidForPlayer="+PhaseCollisionSystem.Collides(o,world.Player.Entity)+" support="+box.referenceSupportId+" side="+box.referenceSupportSide,world);
                else if(o.kind==ObjectKind.Terrain) Debug.Log("[MirrorTerrainLive] "+o.objectId+" center="+o.Position.ToString("F6")+" bounds="+o.Bounds,world);
            }
        }
        public void DumpLastAudit()
        {
            foreach(var entry in world.LastMirrorAudit) Debug.Log("[MirrorAudit] "+entry.box+" color="+entry.colorBefore+"->"+entry.colorAfter+" participating="+entry.participated+" "+JsonUtility.ToJson(entry),world);
        }
    }
}







