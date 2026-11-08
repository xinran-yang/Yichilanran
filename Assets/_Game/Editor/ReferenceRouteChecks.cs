using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace YiChiLanRan.Editor
{
    public static class ReferenceRouteChecks
    {
        public static void BuildAndRun()
        { LevelCatalog.RebuildReferences(); Run("2.1",Route21); Run("2.2",Route22); }
        public static void FinalizeDemo() { BuildAndRun(); SupportMirrorVerification.BuildAndRun(); }
        static void Dive(RouteDriver b,float x,float delay=.24f)
        {
            b.Walk(x); b.Wait(.15f); int n=b.world.Transfers;
            b.Tick(0,true); for(int i=0;i<delay/Rules.Step;i++) b.Tick(); b.Tick(0,false,true);
            for(int i=0;i<260;i++) { b.Tick(); if(b.world.Transfers>n) { b.Wait(1.0f); return; } }
            throw new Exception("Dive did not transfer "+b.State());
        }
        static void Route21(RouteDriver b)
        {
            b.Wait(.4f); b.Walk(3.12f); b.Jump(3.9f); b.Jump(4.9f); Dive(b,5.8f);
            b.Walk(11.82f,6); b.Jump(12.6f); b.Jump(13.9f); b.Walk(18.1f); b.Wait(.6f);
            b.Walk(20.82f); b.Jump(21.6f); Dive(b,21.6f);
            b.Walk(22.6f); b.Wait(.5f); b.Walk(19.98f); // Move to the right side, push the blue box LEFT toward the independent ledge.
            b.Jump(19.2f); b.Jump(17.8f); b.Walk(15.1f); b.Jump(16.7f); b.Walk(22.1f);
        }
        static void Route22(RouteDriver b) { Level22RevisionChecks.FollowRoute(b); }
        static void Run(string id,Action<RouteDriver> actions)
        {
            if(!Application.isBatchMode) throw new Exception("Use isolated batch project");
            if(string.IsNullOrEmpty(SceneManager.GetActiveScene().path)) EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            var previous=SceneManager.GetActiveScene(); var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            var oldMode=Physics2D.simulationMode; Physics2D.simulationMode=SimulationMode2D.Script;
            string result=""; RouteDriver bot=null;
            try
            {
                SceneManager.SetActiveScene(scene);
                var d=AssetDatabase.LoadAssetAtPath<LevelDefinition>(GreyboxBuilder.Root+"Data/Levels/Level_"+id.Replace('.','_')+".asset");
                var world=GreyboxBuilder.Create(d); world.Initialize(true); bot=new RouteDriver(world,scene.GetPhysicsScene2D());
                actions(bot);
                if(!world.Complete) throw new Exception("Flag not reached "+bot.State());
                string sequence=string.Join("|",world.EventHistory);
                string expected=id=="2.1" ? "Yellow solo|Green + Box 3" : "Yellow solo|Green 1 solo|Green 1 + Box 1|Green 2 + Box 3";
                if(sequence!=expected || world.Player.Entity.phase!=Phase.Blue || world.Objects.Find(o=>o.objectId=="Box 3").phase!=Phase.Blue) throw new Exception("Route phase/event mismatch");
                if(id=="2.2" && (world.Objects.Find(o=>o.objectId=="Box 1").phase!=Phase.White || world.Objects.Find(o=>o.objectId=="Box 2").phase!=Phase.White)) throw new Exception("Unrelated box phase mismatch");
                result="PASS reference-map route "+id+"\n"+string.Join("\n",world.EventHistory)+"\n"+bot.State();
            }
            catch(Exception e){result="FAIL "+e; throw;}
            finally
            {
                File.WriteAllText("DevReports/ReferenceRoute-"+id+".txt",result); bot?.Save("DevReports/ReferenceRoute-"+id+".csv");
                Physics2D.simulationMode=oldMode; SceneManager.SetActiveScene(previous); EditorSceneManager.CloseScene(scene,true);
            }
        }
    }
}





