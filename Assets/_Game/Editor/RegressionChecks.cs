using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace YiChiLanRan.Editor
{
    public static class RegressionChecks
    {
        static StringBuilder report;
        static void Check(bool condition,string message)
        { report.AppendLine((condition ? "PASS " : "FAIL ")+message); if(!condition) throw new Exception(message); }
        public static void All()
        {
            LevelCatalog.GenerateAll(); RouteChecks.Run21(); RouteChecks.Run22(); Run();
            Debug.Log("[Greybox] All stages passed.");
        }
        public static void Reset(LevelRuntime w,RouteDriver bot)
        {
            w.ResetLevel();
            if(w.Busy || w.Complete || w.Mirrored || w.Transfers!=0 || w.Player.Diving || w.EventHistory.Count!=0 || w.Player.Entity.phase!=Phase.Blue) throw new Exception("Reset failed to restore state");
            foreach(var spec in w.definition.boxes)
            {
                var box=w.Objects.Find(o=>o.objectId==spec.id);
                if(box.phase!=spec.phase || Vector2.Distance(box.Body.position,spec.center)>.001f || box.Body.linearVelocity!=Vector2.zero) throw new Exception("Reset failed: "+spec.id);
            }
            w.HarnessLock();
            w.ResetLevel();
            foreach(var o in w.Objects) if(o.Body && !o.Body.simulated) throw new Exception("Reset left physics disabled");
            bot.Wait(.7f);
            if(!w.Player.Grounded) throw new Exception("Reset did not restore playable spawn");
        }
        static void Fixture(LevelDefinition d,Action<RouteDriver> test)
        {
            var oldMode=Physics2D.simulationMode; Physics2D.simulationMode=SimulationMode2D.Script;
            if(string.IsNullOrEmpty(SceneManager.GetActiveScene().path)) EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            var previous=SceneManager.GetActiveScene(); var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene); var w=GreyboxBuilder.Create(d); w.Initialize(true);
                test(new RouteDriver(w,scene.GetPhysicsScene2D()));
            }
            finally { Physics2D.simulationMode=oldMode; if(previous.IsValid()) SceneManager.SetActiveScene(previous); EditorSceneManager.CloseScene(scene,true); }
        }
        public static void Run()
        {
            if(!Application.isBatchMode) throw new Exception("Run regression in isolated batch copy");
            report=new StringBuilder("Unity "+Application.unityVersion+" targeted regression\n");
            try
            {
                foreach(var size in new[]{new Vector2Int(1920,1080),new Vector2Int(1600,900),new Vector2Int(1280,720),new Vector2Int(1366,768),new Vector2Int(800,600)})
                {
                    var viewport=LevelRuntime.ViewportFor(size.x,size.y);
                    Check(Mathf.Abs(size.x*viewport.width/(size.y*viewport.height)-16f/9f)<.0001f,"Viewport preserves 16:9 at "+size);
                }
                Check(24<7.6f*2*16/9f && 12<7.6f*2,"Full world fits unchanged orthographic view with HUD margins");
                Fixture(LevelCatalog.Level22(),b=>
                {
                    b.Wait(.5f); b.Walk(7); b.Dive(7);
                    b.Walk(13.68f); float maxFoot=0;
                    for(int i=0;i<720;i++)
                    {
                        bool jump=b.world.Player.Grounded && i%24==0;
                        bool dive=!b.world.Player.Grounded && i%48==30;
                        b.Tick(1,jump,dive); maxFoot=Mathf.Max(maxFoot,b.world.Player.Entity.Bounds.yMin);
                    }
                    Check(b.world.Transfers==1 && b.world.Player.Entity.phase==Phase.White && b.world.Player.Entity.Body.position.x<14,"G1 repeated white jumps/dives cannot reach right ledge/Green 2 after first yellow");
                    Check(maxFoot<9.95f,"G1 measured feet peak "+maxFoot.ToString("F3")+" below right ledge 9.950");
                    Check(!b.world.Objects.Find(o=>o.objectId=="Box 2").elastic,"White filler box cannot provide blue bounce");
                });
                var simple=ScriptableObject.CreateInstance<LevelDefinition>(); simple.levelId="Barrier fixture"; simple.spawn=new Vector2(2,2.40f); simple.goal=new Vector2(22,11);
                GreyboxBuilder.Boundaries(simple); simple.terrain.Add(new ShapeSpec("Floor",new Rect(.1f,.1f,23.8f,1.9f),Phase.Blue,true));
                simple.terrain.Add(new ShapeSpec("Box-only barrier",new Rect(5,2,.12f,4),Phase.Neutral,false,ObjectKind.BoxBarrier));
                simple.boxes.Add(new BoxSpec("Box 1",4,2.02f,Phase.Blue)); simple.boxes.Add(new BoxSpec("Box 2",20,.12f,Phase.White)); simple.boxes.Add(new BoxSpec("Box 3",22,.12f,Phase.White));
                Fixture(simple,b=>
                {
                    b.Wait(.5f); for(int i=0;i<300;i++) b.Tick(1);
                    Check(b.world.Objects.Find(o=>o.objectId=="Box 1").Bounds.xMax<=5.015f,"G3 real physics push cannot pass box-only barrier");
                    b.Jump(6.2f);
                    Check(b.world.Player.Entity.Bounds.xMin>5.12f,"G3 player crosses same barrier with normal input");
                });
                Fixture(LevelCatalog.Level22(),b=>
                {
                    b.Wait(.5f); b.Walk(13.68f);
                    for(int i=0;i<720;i++) b.Tick(-1,b.world.Player.Grounded && i%30==0,!b.world.Player.Grounded && i%45==30);
                    Check(b.world.Transfers==0 && b.world.Player.Entity.Bounds.yMin<4.1f,"Initial blue jump/dive attempts do not access upper Green 1 shelf");
                });
                var d=LevelCatalog.Level22();
                var roof=d.terrain.Find(t=>t.id=="Box 3 low blue roof");
                Check(roof.rect.yMin-(.1f+Rules.BoxSize)<Rules.PlayerHeight,"G4 box 3 initial roof clearance smaller than upright player");
                Check(roof.rect.yMax+Rules.Bounce*Rules.Bounce/(2*Rules.Gravity)+.02f<4.25f,"G4 even roof-assisted blue bounce cannot reach Green 2 upper shelf");
                report.AppendLine("LIMIT These are targeted physical attempts plus geometric bounds, not an exhaustive proof against every possible input sequence.");
                report.AppendLine("LIMIT Headless run verifies viewport calculations, not desktop window appearance or human keyboard feel.");
            }
            finally { Directory.CreateDirectory("DevReports"); File.WriteAllText("DevReports/Regression.txt",report.ToString()); }
        }
    }
}

