using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace YiChiLanRan.Editor
{
    public static class MirrorPlaySmoke
    {
        static int stage,caseIndex;
        static double started;
        static float nextTime;
        static bool oldEnabled;
        static EnterPlayModeOptions oldOptions;
        static readonly StringBuilder report=new StringBuilder();
        static readonly Dictionary<PhaseObject,Vector2> locked=new Dictionary<PhaseObject,Vector2>();
        static readonly Dictionary<Rigidbody2D,bool> simulated=new Dictionary<Rigidbody2D,bool>();
        static readonly Dictionary<Rigidbody2D,RigidbodyInterpolation2D> interpolations=new Dictionary<Rigidbody2D,RigidbodyInterpolation2D>();
        static readonly Dictionary<Rigidbody2D,float> gravity=new Dictionary<Rigidbody2D,float>();
        static LevelRuntime world;
        static void Check(bool value,string message)
        { if(!value) throw new Exception(message); report.AppendLine("PASS "+message); }
        public static void Start()
        {
            report.Clear(); report.AppendLine("Unity "+Application.unityVersion+" actual Play Mode mirror lock checks");
            oldEnabled=EditorSettings.enterPlayModeOptionsEnabled; oldOptions=EditorSettings.enterPlayModeOptions;
            EditorSettings.enterPlayModeOptionsEnabled=true;
            EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload|EnterPlayModeOptions.DisableSceneReload;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            world=GreyboxBuilder.Create(MirrorInvariantChecks.Fixture());
            world.Objects.Find(o=>o.objectId=="Box 3").Body.simulated=false;
            // Capture authored initial state including the deliberately disabled box.
            world.Initialize();
            started=EditorApplication.timeSinceStartup; stage=-1; caseIndex=0;
            Application.logMessageReceived+=Log; EditorApplication.update+=Tick;
        }
        static void Log(string message,string stack,LogType type)
        {
            if(stack.Contains("UnityEditor.Search.SearchDatabase") && !stack.Contains("YiChiLanRan"))
            { report.AppendLine("EDITOR INFRASTRUCTURE "+message); return; }
            if(type==LogType.Exception || type==LogType.Assert || type==LogType.Error) Finish(false,message+"\n"+stack);
        }
        static void Tick()
        {
            try
            {
                if(EditorApplication.timeSinceStartup-started>50) throw new Exception("Timeout case="+caseIndex+" stage="+stage);
                if(stage==-1)
                { if(EditorApplication.timeSinceStartup-started>1) { stage=0; EditorApplication.EnterPlaymode(); } return; }
                if(!EditorApplication.isPlaying || world.Player==null) return;
                if(stage==0)
                {
                    if(world.Clock<.1f) return;
                    Check(!world.Harness,"Case "+caseIndex+" uses production FixedUpdate and animated transaction");
                    world.ResetLevel(); world.Player.ExternalInput=true;
                    var box=world.Objects.Find(o=>o.objectId=="Box 1");
                    float x=caseIndex==1 ? 7.7f : caseIndex==2 ? box.Bounds.center.x : 20;
                    float bottom=caseIndex==2 ? box.Bounds.yMax : 3;
                    world.Player.Entity.Place(new Vector2(x,bottom+Rules.PlayerHeight/2+.02f));
                    stage=1;
                }
                else if(stage==1 && world.Clock>.14f)
                {
                    Check(world.Player.Grounded,"Case "+caseIndex+" begins on legitimate support");
                    world.Player.Command(0,true); nextTime=world.Clock+.16f; stage=2;
                }
                else if(stage==2 && world.Clock>=nextTime)
                {
                    simulated.Clear(); interpolations.Clear(); gravity.Clear();
                    foreach(var o in world.Objects) if(o.Body)
                    { simulated[o.Body]=o.Body.simulated; interpolations[o.Body]=o.Body.interpolation; gravity[o.Body]=o.Body.gravityScale; }
                    world.Player.Command(0,false,true); stage=3;
                }
                else if(stage==3 && world.Busy)
                {
                    locked.Clear();
                    foreach(var o in world.Objects)
                        if(o.Body)
                        {
                            if(o.Body.simulated || o.Body.linearVelocity!=Vector2.zero || o.Body.gravityScale!=0) throw new Exception("Body not frozen: "+o.objectId);
                            locked[o]=o.Position;
                        }
                    Check(true,"Case "+caseIndex+" freezes all bodies and clears gravity/velocity before transform");
                    if(caseIndex==3) { world.ResetLevel(); stage=7; }
                    else stage=4;
                }
                else if(stage==4)
                {
                    if(world.Transfers==0)
                    {
                        foreach(var pair in locked) if(Vector2.Distance(pair.Key.Position,pair.Value)>.00001f) throw new Exception("Body drifted during pre-commit animation");
                    }
                    else
                    {
                        Check(world.Busy,"Case "+caseIndex+" remains locked through post-commit animation");
                        Check(world.LastMirrorAudit.Count==3,"Case "+caseIndex+" audits every box, including disabled body");
                        foreach(var a in world.LastMirrorAudit) Check(a.formulaVerified && a.moment=="COMMIT_FROZEN","Case "+caseIndex+" exact committed pose "+a.box);
                        if(caseIndex==4) { world.ResetLevel(); stage=7; }
                        else stage=5;
                    }
                }
                else if(stage==5 && !world.Busy)
                {
                    Check(!world.Mirror.HasPhysicsLock,"Case "+caseIndex+" releases transaction lock");
                    foreach(var pair in simulated)
                        if(pair.Key.simulated!=pair.Value || pair.Key.interpolation!=interpolations[pair.Key] || pair.Key.gravityScale!=gravity[pair.Key]) throw new Exception("Body settings not restored");
                    Check(true,"Case "+caseIndex+" restores simulation/interpolation/gravity, including disabled box");
                    foreach(var a in world.LastMirrorAudit) Check(a.formulaVerified && a.moment=="RESTORED_BEFORE_STEP","Case "+caseIndex+" verifies pose at resume "+a.box);
                    var box=world.Objects.Find(o=>o.objectId=="Box 1");
                    Check(box.phase==(caseIndex==2 ? Phase.White:Phase.Blue),"Case "+caseIndex+" box color independent of geometry");
                    Check(world.Player.Entity.phase==Phase.White && world.Player.Entity.Body.rotation==0,"Case "+caseIndex+" safe player landing stays upright");
                    Check(Physics2D.GetIgnoreCollision(world.Player.Entity.Shape,box.Shape)==!PhaseCollisionSystem.Collides(world.Player.Entity,box),"Case "+caseIndex+" has correct native collision filters after resume");
                    nextTime=world.Clock+.2f; stage=6;
                }
                else if(stage==6 && world.Clock>=nextTime)
                {
                    var box=world.Objects.Find(o=>o.objectId=="Box 1"); var a=world.LastMirrorAudit.Find(e=>e.box=="Box 1");
                    Check(box.Position.y<a.after.y-.02f,"Case "+caseIndex+" unsupported box falls only after normal simulation resumes (not a mirror snap)");
                    Next();
                }
                else if(stage==7 && world.Clock>.5f)
                {
                    Check(!world.Busy && !world.Mirror.HasPhysicsLock && !world.Mirrored && world.Transfers==0,"Reset cancels pending mirror at stage "+(caseIndex==3 ? "before commit":"after commit"));
                    Check(world.Player.Entity.phase==Phase.Blue && !world.Objects.Find(o=>o.objectId=="Box 3").Body.simulated,"Reset restores source color and disabled-body state");
                    Next();
                }
            }
            catch(Exception e) { Finish(false,e.ToString()); }
        }
        static void Next()
        {
            caseIndex++;
            if(caseIndex>=5)
            {
                report.AppendLine("LIMIT This fixture tests actual production animation and physics lifecycle, not the currently blocked puzzle routes.");
                Finish(true,""); return;
            }
            world.ResetLevel(); stage=0;
        }
        static void Finish(bool ok,string reason)
        {
            EditorApplication.update-=Tick; Application.logMessageReceived-=Log;
            EditorSettings.enterPlayModeOptionsEnabled=oldEnabled; EditorSettings.enterPlayModeOptions=oldOptions;
            if(!ok) report.AppendLine("FAIL "+reason);
            Directory.CreateDirectory("DevReports"); File.WriteAllText("DevReports/MirrorFix-PlayMode.txt",report.ToString());
            EditorApplication.Exit(ok ? 0:1);
        }
    }
}
