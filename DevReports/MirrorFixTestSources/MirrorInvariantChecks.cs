using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace YiChiLanRan.Editor
{
    public static class MirrorInvariantChecks
    {
        static readonly StringBuilder report=new StringBuilder();
        static readonly StringBuilder csv=new StringBuilder();
        static readonly CultureInfo CI=CultureInfo.InvariantCulture;
        public static void Full() { Run(); MirrorPlaySmoke.Start(); }
        static void Check(bool condition,string message)
        { report.AppendLine((condition ? "PASS " : "FAIL ")+message); if(!condition) throw new Exception(message); }
        public static LevelDefinition Fixture(float axis=6,Phase source=Phase.Blue)
        {
            var d=ScriptableObject.CreateInstance<LevelDefinition>(); d.levelId="Mirror invariant fixture"; d.mirrorAxis=axis;
            d.bounds=new Rect(0,-50,24,130); d.spawn=new Vector2(20,3.40f); d.goal=new Vector2(23,25);
            d.terrain.Add(new ShapeSpec("Green support",new Rect(5,2.8f,4,.2f),source,source==Phase.Blue));
            d.terrain.Add(new ShapeSpec("Yellow support",new Rect(19,2.8f,3,.2f),source,source==Phase.Blue));
            d.terrain.Add(new ShapeSpec("Remote neutral catch",new Rect(.1f,-20,23.8f,.2f),Phase.Neutral));
            d.boxes.Add(new BoxSpec("Box 1",6,3.02f,source));
            d.boxes.Add(new BoxSpec("Box 2",12,5.5f,Rules.Opposite(source)));
            d.boxes.Add(new BoxSpec("Box 3",15,7.5f,source));
            d.zones.Add(new ZoneSpec("Yellow",19,3,3,false));
            d.zones.Add(new ZoneSpec("Green",5,4,3,true));
            return d;
        }
        public static void WithFixture(LevelDefinition d,Action<LevelRuntime,PhysicsScene2D> test)
        {
            var mode=Physics2D.simulationMode; Physics2D.simulationMode=SimulationMode2D.Script;
            if(string.IsNullOrEmpty(SceneManager.GetActiveScene().path)) EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            var previous=SceneManager.GetActiveScene(); var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene); var w=GreyboxBuilder.Create(d); w.Initialize(true);
                test(w,scene.GetPhysicsScene2D());
            }
            finally { Physics2D.simulationMode=mode; if(previous.IsValid()) SceneManager.SetActiveScene(previous); EditorSceneManager.CloseScene(scene,true); }
        }
        static void Prime(LevelRuntime w,PhysicsScene2D physics,Vector2 playerPosition)
        {
            for(int i=0;i<25;i++) { w.Player.Command(0); w.Tick(Rules.Step); physics.Simulate(Rules.Step); }
            w.Player.Entity.Place(playerPosition); w.Player.Command(0,false,true); w.Tick(Rules.Step); physics.Simulate(Rules.Step);
            Check(w.Player.Diving,"Dive fixture primed");
        }
        static void SaveAudit(LevelRuntime w,string label)
        {
            foreach(var a in w.LastMirrorAudit)
            {
                csv.Append(label).Append(',').Append(a.transaction).Append(',').Append(a.moment).Append(',').Append(a.axis.ToString("R",CI)).Append(',').Append(a.box).Append(',').Append(a.participated).Append(',')
                    .Append(a.before.x.ToString("R",CI)).Append(',').Append(a.before.y.ToString("R",CI)).Append(',').Append(a.expected.x.ToString("R",CI)).Append(',').Append(a.expected.y.ToString("R",CI)).Append(',')
                    .Append(a.after.x.ToString("R",CI)).Append(',').Append(a.after.y.ToString("R",CI)).Append(',').Append(a.colorBefore).Append(',').Append(a.colorAfter).Append(',')
                    .Append(a.positionError.ToString("R",CI)).Append(',').Append(a.colliderError.ToString("R",CI)).Append(',').Append(a.formulaVerified).Append(',').Append(a.reason).AppendLine();
            }
        }
        public static void Run()
        {
            if(!Application.isBatchMode) throw new Exception("Run mirror checks in isolated batch copy");
            report.Clear(); report.AppendLine("Unity "+Application.unityVersion+" strict mirror regression");
            csv.Clear(); csv.AppendLine("case,transaction,moment,axis,box,participating,beforeX,beforeY,expectedX,expectedY,afterX,afterY,beforeColor,afterColor,bodyError,colliderError,verified,reason");
            try
            {
                foreach(float axis in new[]{-2.75f,6f,11.125f})
                    foreach(Phase source in new[]{Phase.Blue,Phase.White})
                        foreach(string kind in new[]{"yellow","green-solo","green-box"})
                            Matrix(axis,source,kind,false);
                Matrix(6,Phase.Blue,"green-box",true);
                DoubleMirror(); UnsafeAndStale(); MeasureJumps(); MapCompatibility();
            }
            finally
            {
                Directory.CreateDirectory("DevReports"); File.WriteAllText("DevReports/MirrorFixTests.txt",report.ToString());
                File.WriteAllText("DevReports/MirrorAudit.csv",csv.ToString());
            }
            Debug.Log("[MirrorFix] Strict mirror checks passed. Existing-map conflicts are reported separately, not counted as playable-route passes.");
        }
        static void Matrix(float axis,Phase source,string kind,bool offset)
        {
            WithFixture(Fixture(axis,source),(w,physics)=>
            {
                var box=w.Objects.Find(o=>o.objectId=="Box 1"); var player=w.Player.Entity;
                player.SetPhase(source);
                if(offset)
                {
                    box.Shape.offset=new Vector2(.13f,.17f); box.transform.localScale=new Vector3(1.2f,1.2f,1);
                    box.Place(new Vector2(6,3.02f+box.WorldSize.y/2-box.WorldOffset(box.Shape.offset).y));
                }
                PhaseCollisionSystem.Refresh(w); Physics2D.SyncTransforms();
                var zone=w.Zones.Find(z=>z.data.id==(kind=="yellow" ? "Yellow":"Green"));
                Prime(w,physics,new Vector2(kind=="green-box" ? box.Bounds.center.x : kind=="yellow" ? 20:7.7f,kind=="green-box" ? box.Bounds.yMax+1.2f : 5));
                var before=new Dictionary<PhaseObject,Vector2>(); var centers=new Dictionary<PhaseObject,Vector2>(); var colors=new Dictionary<PhaseObject,Phase>();
                foreach(var o in w.Objects) { before[o]=o.Position; centers[o]=o.Bounds.center; colors[o]=o.phase; }
                var inactive=w.Objects.Find(o=>o.objectId=="Box 3"); inactive.Body.simulated=false;
                box.Body.gravityScale=.75f; // Configuration must survive the lock, but cannot displace the frozen commit.
                var constraints=box.Body.constraints; var interpolation=box.Body.interpolation;
                Check(w.Mirror.TryTransfer(zone,kind=="green-box" ? box:null),"Transfer accepted: "+kind+" axis="+axis+" source="+source+" offset="+offset);
                foreach(var o in w.Objects)
                {
                    if(o.kind==ObjectKind.Player) continue; // Player explicitly retains safe landing.
                    Check(Vector2.Distance(o.Position,Rules.Mirror(before[o],axis))<.00001f,"Exact pivot mirror: "+o.objectId);
                    Check(Vector2.Distance(o.Bounds.center,Rules.Mirror(centers[o],axis))<.00001f,"Exact collider-center mirror: "+o.objectId);
                    Check(o.phase==(o==box && kind=="green-box" ? Rules.Opposite(colors[o]):colors[o]),"Phase independent from geometry: "+o.objectId);
                }
                Check(w.LastMirrorAudit.Count==3,"All blue and white boxes audited");
                foreach(var a in w.LastMirrorAudit) Check(a.formulaVerified && a.moment=="RESTORED_BEFORE_STEP","Verified frozen/resume audit: "+a.box);
                Check(box.Body.gravityScale==.75f && box.Body.constraints==constraints && box.Body.interpolation==interpolation && !inactive.Body.simulated,"Rigidbody configuration restored, disabled body remains disabled");
                Check(!w.Busy && !w.Mirror.HasPhysicsLock && player.Body.rotation==0,"Physics lock released; player remains upright");
                Check(player.Body.linearVelocity==Vector2.zero && box.Body.linearVelocity==Vector2.zero,"No pre-transfer velocity leaks into commit");
                Check(Physics2D.GetIgnoreCollision(player.Shape,box.Shape)==!PhaseCollisionSystem.Collides(player,box),"Collision filtering reapplied after simulation resumes");
                SaveAudit(w,kind+"-"+source+"-"+axis.ToString(CI)+(offset ? "-offset-scaled":""));
            });
        }
        static void DoubleMirror()
        {
            WithFixture(Fixture(),(w,physics)=>
            {
                var box=w.Objects.Find(o=>o.objectId=="Box 1"); box.Shape.offset=new Vector2(.12f,.1f); box.Place(new Vector2(6,3.42f));
                Prime(w,physics,new Vector2(20,5));
                var before=new Dictionary<PhaseObject,Vector2>(); foreach(var o in w.Objects) before[o]=o.Position;
                var zone=w.Zones.Find(z=>z.data.id=="Yellow"); Check(w.Mirror.TryTransfer(zone,null),"Double mirror first transfer");
                for(int i=0;i<25;i++) w.Tick(Rules.Step); // No physics integration: compare the geometric involution alone.
                w.Player.ResetController(); // New dive fixture, without changing any pose or integrating physics.
                w.Player.Command(0,false,true); w.Tick(Rules.Step);
                Check(w.Mirror.TryTransfer(zone,null),"Double mirror second transfer: "+w.Failure);
                foreach(var o in w.Objects) if(o.kind!=ObjectKind.Player) Check(Vector2.Distance(o.Position,before[o])<.00001f,"Two mirrors restore original pose: "+o.objectId);
                Check(box.Shape.offset==new Vector2(.12f,.1f),"Two mirrors restore collider offset"); SaveAudit(w,"double-mirror");
            });
        }
        static void UnsafeAndStale()
        {
            WithFixture(Fixture(),(w,physics)=>
            {
                Prime(w,physics,new Vector2(6,5.2f)); var box=w.Objects.Find(o=>o.objectId=="Box 1"); var zone=w.Zones.Find(z=>z.data.id=="Green");
                var before=box.Position; var target=Rules.Mirror(box.Bounds,6);
                var obstacle=GreyboxBuilder.Make(w.transform,"Blocked exact target",Rules.Mirror(target,6),Phase.White,ObjectKind.Terrain);
                w.Objects.Add(obstacle); PhaseCollisionSystem.Refresh(w); Physics2D.SyncTransforms();
                Check(!w.Mirror.TryTransfer(zone,box),"Unsafe exact target rejected instead of snapping box above floor");
                Check(w.Transfers==0 && box.Position==before && box.phase==Phase.Blue && !w.Busy && !w.Mirror.HasPhysicsLock,"Unsafe transfer changes no box/world/physics state");
                SaveAudit(w,"unsafe-target");
            });
            WithFixture(Fixture(),(w,physics)=>
            {
                Prime(w,physics,new Vector2(20,5)); var zone=w.Zones.Find(z=>z.data.id=="Yellow");
                Check(w.Mirror.TryPlan(zone,null,out var plan),"Stale-plan fixture prepared");
                var box=w.Objects.Find(o=>o.objectId=="Box 2"); box.Place(box.Position+Vector2.up*.2f);
                Check(!w.Transition(plan) && w.Transfers==0 && !w.Mirror.HasPhysicsLock,"Changed source rejects stale plan without physics side effects");
            });
        }
        static void MeasureJumps()
        {
            var text=new StringBuilder("Unity "+Application.unityVersion+"; actual Rigidbody2D simulation; step="+Rules.Step.ToString("R",CI)+"\n");
            foreach(string type in new[]{"white","blue","bounce"})
                WithFixture(Fixture(),(w,physics)=>
                {
                    var support=w.Objects.Find(o=>o.objectId=="Yellow support"); var p=w.Player.Entity;
                    support.SetPhase(type=="white" ? Phase.White:Phase.Blue); support.elastic=type!="white"; p.SetPhase(support.phase); PhaseCollisionSystem.Refresh(w);
                    p.Place(new Vector2(20,3.4f));
                    for(int i=0;i<100;i++) { w.Tick(Rules.Step); physics.Simulate(Rules.Step); }
                    float start=p.Bounds.yMin,peak=start;
                    if(type=="bounce") w.Player.Bounce(w.Clock); else w.Player.Command(0,true);
                    for(int i=0;i<180;i++) { w.Tick(Rules.Step); physics.Simulate(Rules.Step); peak=Mathf.Max(peak,p.Bounds.yMin); }
                    float velocity=type=="white" ? Rules.Jump : type=="blue" ? Rules.BlueJump : Rules.Bounce;
                    text.AppendLine(type+" measured="+(peak-start).ToString("F6",CI)+" theoretical="+(velocity*velocity/(2*Rules.Gravity)).ToString("F6",CI));
                    float theoretical=velocity*velocity/(2*Rules.Gravity);
                    Check(peak-start>theoretical-.12f && peak-start<=theoretical+.02f,"Measured actual jump: "+type+" "+(peak-start).ToString("F6",CI));
                });
            File.WriteAllText("DevReports/JumpMeasurements.txt",text.ToString());
        }
        static void MapCompatibility()
        {
            var text=new StringBuilder("Current map compatibility with strict box reflection. No terrain or scene assets modified.\n");
            Probe(LevelCatalog.Level21(),"Green",true,Phase.White,"Box 3",text);
            Probe(LevelCatalog.Level22(),"Green 1",false,Phase.Blue,"Box 1",text);
            Probe(LevelCatalog.Level22(),"Green 2",true,Phase.White,"Box 3",text);
            text.AppendLine("BLOCKED These existing green carry targets overlap destination terrain. They must be redrawn; no box snapping or level-specific mirror exceptions were introduced.");
            File.WriteAllText("DevReports/MirrorFix-LevelCompatibility.txt",text.ToString());
        }
        static void Probe(LevelDefinition d,string id,bool mirrored,Phase phase,string boxId,StringBuilder text)
        {
            WithFixture(d,(w,physics)=>
            {
                if(mirrored)
                {
                    foreach(var o in w.Objects) { o.Place(Rules.Mirror(o.Position,d.mirrorAxis)); o.Shape.offset=new Vector2(o.Shape.offset.x,-o.Shape.offset.y); }
                    foreach(var z in w.Zones) z.transform.position=Rules.Mirror((Vector2)z.transform.position,d.mirrorAxis);
                }
                var zone=w.Zones.Find(z=>z.data.id==id); var box=w.Objects.Find(o=>o.objectId==boxId);
                box.SetPhase(phase); box.Place(new Vector2(zone.data.x+zone.data.width/2,zone.Floor+.515f));
                w.Player.Entity.SetPhase(phase); PhaseCollisionSystem.Refresh(w); Physics2D.SyncTransforms();
                Prime(w,physics,new Vector2(box.Position.x,box.Bounds.yMax+1.1f));
                Check(!w.Mirror.TryTransfer(zone,box) && w.Failure.StartsWith("Unsafe exact mirror"),"Current map exposes strict-target overlap: "+d.levelId+" "+id);
                text.AppendLine(d.levelId+" "+id+": "+w.Failure);
                SaveAudit(w,"map-"+d.levelId+"-"+id);
                foreach(var a in w.LastMirrorAudit) if(a.participated) text.AppendLine("axis="+a.axis+" source="+a.before.ToString("F6")+" exactTarget="+a.expected.ToString("F6")+" unchangedActual="+a.after.ToString("F6"));
            });
        }
    }
}





