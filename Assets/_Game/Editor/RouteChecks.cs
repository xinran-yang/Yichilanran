using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace YiChiLanRan.Editor
{
    public sealed class RouteDriver
    {
        public readonly LevelRuntime world;
        readonly PhysicsScene2D physics;
        readonly StringBuilder trace=new StringBuilder("tick,x,y,vx,vy,phase,grounded,transfers,box1x,box1y,box2x,box2y,box3x,box3y\n");
        int ticks;
        public RouteDriver(LevelRuntime world,PhysicsScene2D physics) {this.world=world; this.physics=physics;}
        public void Tick(float axis=0,bool jump=false,bool dive=false)
        {
            world.Player.Command(axis,jump,dive); world.Tick(Rules.Step); physics.Simulate(Rules.Step);
            if(ticks++%12==0)
            {
                var p=world.Player.Entity; var v=p.Body.linearVelocity;
                trace.Append(ticks).Append(',').Append(p.Body.position.x.ToString("F4",System.Globalization.CultureInfo.InvariantCulture)).Append(',').Append(p.Body.position.y.ToString("F4",System.Globalization.CultureInfo.InvariantCulture)).Append(',').Append(v.x).Append(',').Append(v.y).Append(',').Append(p.phase).Append(',').Append(world.Player.Grounded).Append(',').Append(world.Transfers);
                foreach(string id in new[]{"Box 1","Box 2","Box 3"}) {var b=world.Objects.Find(o=>o.objectId==id); trace.Append(',').Append(b.Body.position.x).Append(',').Append(b.Body.position.y);}
                trace.AppendLine();
            }
            foreach(var a in world.Objects)
                if(a.Body)
                    foreach(var b in world.Objects)
                        if(a!=b && b.gameObject.activeSelf && PhaseCollisionSystem.Collides(a,b) && Rules.Overlap(a.Bounds,b.Bounds,.055f)) throw new Exception("Physics penetration: "+a.objectId+" / "+b.objectId+" at "+a.Bounds+" / "+b.Bounds);
        }
        public void Wait(float seconds) { for(int i=0;i<seconds/Rules.Step;i++) Tick(); }
        public void Walk(float x,float seconds=5)
        {
            for(int i=0;i<seconds/Rules.Step;i++)
            {
                if(world.Complete) return;
                float dx=x-world.Player.Entity.Body.position.x;
                if(Mathf.Abs(dx)<.045f) { Tick(); return; }
                Tick(Mathf.Sign(dx));
            }
            throw new Exception("Walk timeout to "+x+" "+State());
        }
        public void Jump(float x,float seconds=2)
        {
            float dx=x-world.Player.Entity.Body.position.x;
            Tick(Mathf.Abs(dx)<.04f ? 0 : Mathf.Sign(dx),true);
            for(int i=0;i<seconds/Rules.Step;i++)
            {
                if(world.Complete) return;
                dx=x-world.Player.Entity.Body.position.x;
                Tick(Mathf.Abs(dx)<.045f ? 0 : Mathf.Sign(dx));
                if(i>15 && world.Player.Grounded && Mathf.Abs(dx)<.05f) return;
            }
            throw new Exception("Jump timeout to "+x+" "+State());
        }
        public void Dive(float x)
        {
            Walk(x); Wait(.18f); int before=world.Transfers;
            Tick(0,true);
            for(int i=0;i<18;i++) Tick();
            Tick(0,false,true);
            for(int i=0;i<240;i++) {Tick(); if(world.Transfers>before) { Wait(1.1f); return; }}
            throw new Exception("Dive failed at "+x+" "+State());
        }
        public string State() => "player="+world.Player.Entity.Bounds+" grounded="+world.Player.Grounded+" phase="+world.Player.Entity.phase+" transfers="+world.Transfers+" event="+world.LastEvent+" rejection="+world.Failure;
        public void Save(string path) { File.WriteAllText(path,trace.ToString()); }
    }
    public static class RouteChecks
    {
        public static void Stage21()
        { GreyboxBuilder.Sandbox(); LevelCatalog.Generate21(); Run21(); }
        public static void Run21() { Run("2.1",GreyboxBuilder.Root+"Data/Levels/Level_2_1.asset",Route21); }
        public static void Run(string id,string asset,Action<RouteDriver> route)
        {
            if(!Application.isBatchMode) throw new InvalidOperationException("Physics regression must run in an isolated batch project.");
            var oldMode=Physics2D.simulationMode; Physics2D.simulationMode=SimulationMode2D.Script;
            if(string.IsNullOrEmpty(SceneManager.GetActiveScene().path)) EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            var previous=SceneManager.GetActiveScene(); var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            RouteDriver bot=null; string result="";
            try
            {
                SceneManager.SetActiveScene(scene);
                var definition=AssetDatabase.LoadAssetAtPath<LevelDefinition>(asset);
                var w=GreyboxBuilder.Create(definition); w.Initialize(true); bot=new RouteDriver(w,scene.GetPhysicsScene2D());
                route(bot);
                if(!w.Complete) throw new Exception("Route did not reach flag: "+bot.State());
                result="PASS Unity Rigidbody2D full input-driven route "+id+"\n"+string.Join("\n",w.EventHistory)+"\n"+bot.State();
                RegressionChecks.Reset(w,bot); result+="\nPASS reset after completion and while transfer-locked restores complete initial state";
            }
            catch(Exception e) { result="FAIL "+e; throw; }
            finally
            {
                Directory.CreateDirectory("DevReports"); File.WriteAllText("DevReports/Route-"+id+".txt",result);
                bot?.Save("DevReports/Route-"+id+".csv");
                Physics2D.simulationMode=oldMode; if(previous.IsValid()) SceneManager.SetActiveScene(previous); EditorSceneManager.CloseScene(scene,true);
            }
            Debug.Log("[Greybox] "+result);
        }
        public static void Stage22()
        { LevelCatalog.Generate22(); Run22(); GreyboxBuilder.Configure(); }
        public static void Run22() { Run("2.2",GreyboxBuilder.Root+"Data/Levels/Level_2_2.asset",Route22); }
        static void Route22(RouteDriver b)
        {
            b.Wait(.6f); b.Walk(7.0f); b.Dive(7.0f); b.Dive(5.3f);
            b.Jump(3.5f); b.Jump(1.0f); b.Walk(3.9f); b.Wait(.6f);
            float boxX=b.world.Objects.Find(o=>o.objectId=="Box 1").Body.position.x;
            b.Jump(boxX); b.Dive(boxX);
            var filler=b.world.Objects.Find(o=>o.objectId=="Box 2");
            if(Mathf.Abs(filler.Bounds.yMax-7.9f)>.04f || Mathf.Abs(filler.Body.position.x-10.54f)>.03f) throw new Exception("G2 box 2 did not automatically fill pit");
            b.Walk(3.7f); b.Wait(.6f); b.Walk(12.72f,8);
            b.Jump(13.5f); b.Jump(14.8f); b.Walk(17.6f); b.Wait(.6f);
            b.Walk(18.72f); b.Jump(19.5f); b.Dive(19.5f);
            b.Walk(18.7f); b.Wait(.6f); b.Walk(19.72f); b.Jump(20.5f); b.Jump(21.7f); b.Walk(22.5f);
            if(b.world.Transfers!=4 || b.world.Objects.Find(o=>o.objectId=="Box 1").phase!=Phase.White || b.world.Objects.Find(o=>o.objectId=="Box 2").phase!=Phase.White || b.world.Objects.Find(o=>o.objectId=="Box 3").phase!=Phase.Blue) throw new Exception("2.2 final state does not match specification");
            if(string.Join("|",b.world.EventHistory)!="Yellow solo|Green 1 solo|Green 1 + Box 1|Green 2 + Box 3") throw new Exception("Unexpected transfer sequence");
        }
        static void Route21(RouteDriver b)
        {
            b.Wait(.6f);
            b.Walk(2.72f); b.Jump(3.5f); b.Jump(4.8f);
            b.Dive(5.7f);
            b.Walk(6.72f); b.Jump(7.5f); b.Jump(8.8f);
            b.Walk(17.4f); b.Wait(.6f); b.Walk(18.72f); b.Jump(19.5f); b.Dive(19.5f);
            b.Walk(18.7f); b.Wait(.6f); b.Walk(19.72f); b.Jump(20.5f); b.Jump(21.7f); b.Walk(22.5f);
        }
    }
}



