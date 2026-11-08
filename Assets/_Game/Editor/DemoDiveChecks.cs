using System;
using System.IO;
using System.Text;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace YiChiLanRan.Editor
{
    public static class DemoDiveChecks
    {
        public static void BuildAndRun() { GreenBoxColorChecks.BuildAndRun(); CheckLandings(); }
        static void CheckLandings()
        {
            var previous=SceneManager.GetActiveScene();
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            var old=Physics2D.simulationMode; Physics2D.simulationMode=SimulationMode2D.Script;
            var report=new StringBuilder("S rebound disabled; Space jump unchanged. Continuous input from 2.1 spawn.\n"); RouteDriver b=null;
            try
            {
                SceneManager.SetActiveScene(scene); var w=GreyboxBuilder.Create(LevelCatalog.Level21()); w.Initialize(true);
                b=new RouteDriver(w,scene.GetPhysicsScene2D()); b.Wait(.4f);
                Land(b,report,"blue terrain"); b.Walk(2.3f); b.Jump(3.1f); b.Wait(.2f);
                Land(b,report,"blue box 1");
                if(w.Transfers!=0 || w.Player.Diving) throw new Exception("Unexpected transfer or residual Dive");
                report.AppendLine("PASS blue terrain and box land normally, no positive automatic rebound velocity; transfers remain 0.");
            }
            catch(Exception e) {report.AppendLine("FAIL "+e); throw;}
            finally
            {
                File.WriteAllText("DevReports/DemoDive-NoBounce.txt",report.ToString()); b?.Save("DevReports/DemoDive-NoBounce.csv");
                Physics2D.simulationMode=old; SceneManager.SetActiveScene(previous); EditorSceneManager.CloseScene(scene,true);
            }
        }
        static void Land(RouteDriver b,StringBuilder report,string label)
        {
            b.Tick(0,true); for(int i=0;i<18;i++) b.Tick(); b.Tick(0,false,true);
            float maxUp=b.world.Player.Entity.Body.linearVelocity.y;
            for(int i=0;i<180;i++) {b.Tick(); maxUp=Mathf.Max(maxUp,b.world.Player.Entity.Body.linearVelocity.y);}
            if(maxUp>.15f || !b.world.Player.Grounded) throw new Exception("Automatic rebound on "+label+" vy="+maxUp);
            report.AppendLine("PASS "+label+": max upward velocity after S="+maxUp+", grounded="+b.world.Player.Grounded);
        }
    }
}
