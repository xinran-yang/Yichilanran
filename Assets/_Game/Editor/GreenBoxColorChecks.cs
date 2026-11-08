using System;
using System.IO;
using System.Text;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace YiChiLanRan.Editor
{
    public static class GreenBoxColorChecks
    {
        public static void BuildAndRun() { ReferenceRouteChecks.BuildAndRun(); CheckBoundaries(LevelCatalog.Level21()); CheckBoundaries(Level22Layout.Create()); CheckBox2(); }
        static void CheckBoundaries(LevelDefinition d)
        {
            var previous=SceneManager.GetActiveScene();
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            var report=new StringBuilder("All yellow/green pad widths must lie on the active terrain boundary.\n");
            try
            {
                SceneManager.SetActiveScene(scene); var w=GreyboxBuilder.Create(d); w.Initialize(true);
                d.mirrorInspection=true;
                var yellow=w.Zones.Find(z=>!z.data.green);
                w.Player.Entity.Place(new Vector2(yellow.data.x+.5f,yellow.Floor+Rules.PlayerHeight/2+Rules.Skin));
                for(int state=0;state<3;state++)
                {
                    foreach(var z in w.Zones)
                    {
                        float covered=z.data.x;
                        for(int pass=0;pass<w.Objects.Count;pass++)
                            foreach(var o in w.Objects)
                            {
                                if(o.kind!=ObjectKind.Terrain || o.phase!=w.Player.Entity.phase) continue;
                                var r=o.Bounds;
                                if(Mathf.Abs(r.yMax-z.Floor)<.0001f && r.xMin<=covered+.0001f && r.xMax>covered) covered=r.xMax;
                            }
                        if(covered<z.data.x+z.data.width-.0001f || Mathf.Abs(z.transform.position.y-z.Floor)>.00001f) throw new Exception("Floating / unsupported pad: "+d.levelId+" "+z.data.id+" y="+z.Floor);
                        report.AppendLine("PASS "+w.Player.Entity.phase+" "+z.data.id+" y="+z.Floor+" full width="+z.data.width+" on actual terrain top");
                    }
                    if(state<2 && !w.Mirror.TryInspectionTransfer(yellow)) throw new Exception(w.Failure);
                }
            }
            catch(Exception e) { report.AppendLine("FAIL "+e); throw; }
            finally {File.WriteAllText("DevReports/PortalBoundary-"+d.levelId+".txt",report.ToString()); SceneManager.SetActiveScene(previous); EditorSceneManager.CloseScene(scene,true);}
        }
        public static void CheckBox2()
        {
            if(string.IsNullOrEmpty(SceneManager.GetActiveScene().path)) EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            var previous=SceneManager.GetActiveScene();
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            var mode=Physics2D.simulationMode; Physics2D.simulationMode=SimulationMode2D.Script;
            var report=new StringBuilder("Real box-2 Green-1 regression, continuous input; no mid-route placement.\n"); RouteDriver b=null;
            try
            {
                SceneManager.SetActiveScene(scene); var w=GreyboxBuilder.Create(Level22Layout.Create()); w.Initialize(true);
                b=new RouteDriver(w,scene.GetPhysicsScene2D());
                b.Wait(.4f); b.Jump(3.9f); b.Walk(8.5f); b.Wait(.3f); b.Walk(11.4f); Dive(b,11.4f);
                b.Walk(13.65f); b.Wait(1.1f); b.Jump(11.1f);
                b.Walk(7.33f); b.Wait(.4f);
                var box=w.Objects.Find(o=>o.objectId=="Box 2"); var zone=w.Zones.Find(o=>o.data.id=="Green 1");
                var support=box.GetComponent<TransferBox2D>();
                report.AppendLine("Before dive: box="+box.Bounds+" support="+support.referenceSupportId+" side="+support.referenceSupportSide+" pad="+zone.Floor+" bottomGap="+Mathf.Abs(box.Bounds.yMin-zone.Floor)+" topGap="+Mathf.Abs(box.Bounds.yMax-zone.Floor)+" accepted="+zone.Accepts(box));
                if(support.referenceSupportSide!=1 || !zone.Contains(box.Bounds) || Mathf.Abs(box.Bounds.yMin-zone.Floor)>.055f || !zone.Accepts(box)) throw new Exception("Box support face is not on the corrected pad boundary");
                b.Jump(box.Position.x); Dive(b,box.Position.x,.08f);
                if(box.phase!=Phase.Blue || w.Player.Entity.phase!=Phase.Blue || w.LastEvent!="Green 1 + Box 2") throw new Exception("Box 2 did not change WHITE -> BLUE: "+b.State()+" box="+box.phase);
                report.AppendLine("PASS actual Green 1 + Box 2: WHITE -> BLUE; event="+w.LastEvent);
                AssertOthers(w); AssertAudit(w,box);
                b.Jump(box.Position.x); Dive(b,box.Position.x,.08f);
                if(box.phase!=Phase.White || w.Player.Entity.phase!=Phase.White || w.LastEvent!="Green 1 + Box 2") throw new Exception("Box 2 did not change BLUE -> WHITE");
                AssertOthers(w); AssertAudit(w,box);
                report.AppendLine("PASS same box/pad returns BLUE -> WHITE; unrelated boxes keep colors, exact mirror audit passes both times.");
                w.ResetLevel(); if(box.phase!=Phase.White || w.Player.Entity.phase!=Phase.Blue || w.Transfers!=0) throw new Exception("Reset failed");
                report.AppendLine("PASS reset restores original colors and transfer count.");
            }
            catch(Exception e) { report.AppendLine("FAIL "+e); throw; }
            finally
            {
                File.WriteAllText("DevReports/GreenBoxColor-2.2.txt",report.ToString()); b?.Save("DevReports/GreenBoxColor-2.2.csv");
                Physics2D.simulationMode=mode; SceneManager.SetActiveScene(previous); EditorSceneManager.CloseScene(scene,true);
            }
        }
        static void AssertOthers(LevelRuntime w)
        {
            if(w.Objects.Find(o=>o.objectId=="Box 1").phase!=Phase.Blue || w.Objects.Find(o=>o.objectId=="Box 3").phase!=Phase.White) throw new Exception("Unrelated box changed color");
        }
        static void AssertAudit(LevelRuntime w,PhaseObject box)
        {
            foreach(var a in w.LastMirrorAudit) if(!a.formulaVerified || a.participated!=(a.box==box.objectId)) throw new Exception("Incorrect mirror/color participation audit");
        }
        static void Dive(RouteDriver b,float x,float delay=.24f)
        {
            b.Walk(x); b.Wait(.15f); int n=b.world.Transfers;
            b.Tick(0,true); for(int i=0;i<delay/Rules.Step;i++) b.Tick(); b.Tick(0,false,true);
            for(int i=0;i<260;i++) { b.Tick(); if(b.world.Transfers>n) {b.Wait(1); return;} }
            throw new Exception("Dive failed: "+b.State());
        }
    }
}

