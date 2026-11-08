using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace YiChiLanRan.Editor
{
    public static class GreyboxChecks
    {
        static StringBuilder report;
        static void Check(bool condition,string message)
        { report.AppendLine((condition ? "PASS " : "FAIL ")+message); if(!condition) throw new Exception(message); }
        [MenuItem("Tools/YiChiLanRan/5 Run automated checks")]
        public static void Run()
        {
            report=new StringBuilder("Unity "+Application.unityVersion+" automated checks\n");
            try
            {
                Check(Mathf.Abs(Rules.BoxSize/Rules.PlayerHeight-1.31579f)<.001f,"Square box = 1.316 player heights");
                Check(Rules.Jump*Rules.Jump/(2*Rules.Gravity)>Rules.BoxSize+.1f,"White jump clears box with margin");
                var r=new Rect(2,3,1,2); Check(Rules.Mirror(Rules.Mirror(r,6),6)==r,"Two geometric reflections restore rectangle");
                foreach(Phase p in new[]{Phase.Blue,Phase.White})
                { Check(!Rules.Collides(ObjectKind.Player,p,ObjectKind.Box,Rules.Opposite(p)),"Opposite player/box do not collide: "+p); Check(Rules.Collides(ObjectKind.Player,p,ObjectKind.Terrain,p),"Same phase terrain collides: "+p); }
                Check(!Rules.Collides(ObjectKind.Player,Phase.Blue,ObjectKind.BoxBarrier,Phase.Neutral),"Player passes box barrier");
                Check(Rules.Collides(ObjectKind.Box,Phase.White,ObjectKind.BoxBarrier,Phase.Neutral),"Box stopped by box barrier");
                PhysicsSandbox();
            }
            finally { Directory.CreateDirectory("DevReports"); File.WriteAllText(Application.isBatchMode ? "DevReports/AutomatedChecks.txt" : "DevReports/AutomatedChecks-Editor.txt",report.ToString()); }
            Debug.Log("[Greybox] Automated checks passed. DevReports/AutomatedChecks.txt");
        }
        static void PhysicsSandbox()
        {
            if(!Application.isBatchMode) { report.AppendLine("SKIP physics checks: run Tools/Run-UnityChecks.ps1 in an isolated copy to avoid simulating user scenes."); return; }
            var oldMode=Physics2D.simulationMode; Physics2D.simulationMode=SimulationMode2D.Script;
            var previous=SceneManager.GetActiveScene(); var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                var d=AssetDatabase.LoadAssetAtPath<LevelDefinition>(GreyboxBuilder.Root+"Data/Levels/MechanicsSandbox.asset");
                if(!d) { report.AppendLine("SKIP mechanics sandbox not generated"); return; }
                var w=GreyboxBuilder.Create(d); w.Initialize(true); var physics=scene.GetPhysicsScene2D();
                for(int i=0;i<90;i++) { w.Player.Command(0); w.Tick(Rules.Step); physics.Simulate(Rules.Step); }
                report.AppendLine("Diagnostic: player transform="+w.Player.transform.position+" body="+w.Player.Entity.Body.position+" velocity="+w.Player.Entity.Body.linearVelocity+" rect="+w.Player.Entity.Bounds+" support="+(w.Support(w.Player.Entity)?.objectId??"none"));
                Check(w.Player.Grounded,"Rigidbody2D player settles on blue floor");
                float y=w.Player.transform.position.y,peak=y;
                w.Player.Command(0,true);
                for(int i=0;i<150;i++) { w.Tick(Rules.Step); physics.Simulate(Rules.Step); peak=Mathf.Max(peak,w.Player.transform.position.y); }
                Check(peak-y>1.6f,"Real 2D physics blue jump has expected height");
                Check(w.Player.Entity.phase==Phase.Blue && w.Transfers==0,"Normal jump never transfers");
                var p=w.Player.Entity; p.Place(new Vector2(7.8f,3.4f)); w.Player.Command(0,false,true);
                for(int i=0;i<100 && w.Transfers==0;i++) { w.Tick(Rules.Step); physics.Simulate(Rules.Step); }
                Check(w.Transfers==1 && w.Mirrored && p.phase==Phase.White,"Yellow Dive performs mirror and player phase change");
                Check(w.Objects.Find(o=>o.objectId=="Box 1").phase==Phase.Blue && w.Objects.Find(o=>o.objectId=="Box 2").phase==Phase.White,"Yellow preserves both box phases");
                Check(w.EventHistory.Count==1,"Single Dive transfers only once");
                for(int i=0;i<100;i++) { w.Tick(Rules.Step); physics.Simulate(Rules.Step); }
                Check(w.Player.Grounded,"Player safely lands after mirror");
                var box=w.Objects.Find(o=>o.objectId=="Box 2");
                for(int i=0;i<160;i++) { w.Tick(Rules.Step); physics.Simulate(Rules.Step); }
                p.Place(new Vector2(15,box.Bounds.yMax+.45f)); w.Player.Command(0,false,true);
                for(int i=0;i<100 && w.Transfers==1;i++) { w.Tick(Rules.Step); physics.Simulate(Rules.Step); }
                Check(w.Transfers==2 && !w.Mirrored && p.phase==Phase.Blue && box.phase==Phase.Blue,"Green Dive transfers only participating same-phase box");
                Check(w.Objects.Find(o=>o.objectId=="Box 1").phase==Phase.Blue,"Unrelated box phase remains unchanged");
                for(int i=0;i<140;i++) { w.Tick(Rules.Step); physics.Simulate(Rules.Step); }
                p.Place(new Vector2(7.8f,3.4f)); w.Player.Command(0,false,true); w.Tick(Rules.Step); physics.Simulate(Rules.Step);
                var zone=w.Zones.Find(z=>z.data.id=="Yellow");
                var block=GreyboxBuilder.Make(w.transform,"Test target obstruction",new Rect(7.4f,.9f,.8f,.8f),Phase.Neutral,ObjectKind.Terrain);
                w.Objects.Add(block); PhaseCollisionSystem.Refresh(w); Physics2D.SyncTransforms();
                int before=w.Transfers; var pos=p.transform.position; var phase=p.phase;
                Check(!w.Mirror.TryPlan(zone,null,out var unsafePlan),"Occupied target rejects transfer");
                Check(w.Transfers==before && p.transform.position==pos && p.phase==phase && !w.Mirrored,"Rejected transaction changes no positions/phases/counters");
                report.AppendLine("NOTE Sandbox jump and yellow transfer use Unity PhysicsScene2D.Simulate; source placement is a fixture, not a full keyboard route.");
            }
            finally { Physics2D.simulationMode=oldMode; if(previous.IsValid()) SceneManager.SetActiveScene(previous); EditorSceneManager.CloseScene(scene,true); }
        }
    }
}





