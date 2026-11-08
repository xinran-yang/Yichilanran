using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace YiChiLanRan.Editor
{
    public static class RealmCorrectionChecks
    {
        public static void FinalizeDemo() { BuildAndRun(); SupportMirrorVerification.BuildAndRun(); }
        public static void BuildAndRun()
        {
            LevelCatalog.RebuildReferences(); Check(LevelCatalog.Level21()); Check(LevelCatalog.Level22());
            ReferenceRouteChecks.BuildAndRun();
            CheckPit();
        }
        public static void CheckPit()
        {
            var previous=SceneManager.GetActiveScene();
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            var mode=Physics2D.simulationMode; Physics2D.simulationMode=SimulationMode2D.Script;
            var report=new StringBuilder("2.1 pit-to-green targeted physics fixture; fixed jump parameters.\n");
            try
            {
                SceneManager.SetActiveScene(scene); var d=LevelCatalog.Level21();
                var w=GreyboxBuilder.Create(d); w.Initialize(true);
                var pit=d.terrain.Find(t=>t.id=="Recess bottom").rect;
                var green=d.terrain.Find(t=>t.id=="Right blue plateau").rect;
                foreach(bool bounce in new[]{false,true})
                {
                    w.ResetLevel(); w.Player.Entity.Place(new Vector2(pit.xMax-.8f,pit.yMax+Rules.PlayerHeight/2+Rules.Skin));
                    var b=new RouteDriver(w,scene.GetPhysicsScene2D()); b.Wait(.3f);
                    float highest=w.Player.Entity.Bounds.yMin; bool entered=false;
                    for(int i=0;i<600;i++)
                    {
                        b.Tick(1,w.Player.Grounded,bounce && !w.Player.Grounded && w.Player.Entity.Body.linearVelocity.y<0);
                        highest=Mathf.Max(highest,w.Player.Entity.Bounds.yMin);
                        entered|=w.Player.Entity.Position.x>green.xMin+Rules.PlayerWidth/2;
                    }
                    report.AppendLine((bounce?"jump + dive bounce":"ordinary jump")+": max feet y="+highest.ToString("F4")+", green top="+green.yMax+", crossed="+entered);
                    if(entered || highest>=green.yMax) throw new Exception("Pit can bypass green ledge");
                }
                report.AppendLine("PASS both attempts blocked by actual geometry in Unity Physics2D.");
            }
            catch(Exception e) {report.AppendLine("FAIL "+e); throw;}
            finally
            {
                File.WriteAllText("DevReports/PitGuard-2.1.txt",report.ToString());
                Physics2D.simulationMode=mode; SceneManager.SetActiveScene(previous); EditorSceneManager.CloseScene(scene,true);
            }
        }
        static void Check(LevelDefinition definition)
        {
            var previous=SceneManager.GetActiveScene(); var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            string result="";
            try
            {
                SceneManager.SetActiveScene(scene); var w=GreyboxBuilder.Create(definition); w.Initialize(true);
                // Geometry transaction fixture; no physics integration between the two reflections.
                var entry=w.Zones[0]; w.Player.Entity.Place(new Vector2(entry.data.x+.4f,entry.Floor+Rules.PlayerHeight/2+Rules.Skin));
                var source=new System.Collections.Generic.Dictionary<PhaseObject,Vector2>();
                foreach(var o in w.Objects) source[o]=o.Position;
                var root=w.Goal.position; var pole=w.Goal.Find("Flag pole"); Vector3 poleBefore=pole.position; var scale=w.Goal.localScale;
                definition.mirrorInspection=true;
                if(!w.Mirror.TryInspectionTransfer(w.Zones[0])) throw new Exception(w.Failure);
                foreach(var o in w.Objects)
                    if(o.kind!=ObjectKind.Player && Vector2.Distance(o.Position,Rules.Mirror(source[o],definition.mirrorAxis))>.00001f) throw new Exception("Wrong terrain/box mirror "+o.objectId);
                if(Vector2.Distance(pole.position,Rules.Mirror((Vector2)poleBefore,definition.mirrorAxis))>.00001f || w.Goal.localScale.y!=-scale.y) throw new Exception("Flag pole not fully reflected");
                foreach(var o in w.Objects) if(o.phase==Phase.White && o.kind==ObjectKind.Terrain && !o.GetComponent<SpriteRenderer>().enabled) throw new Exception("White collision region not visible in white realm");
                if(definition.levelId=="2.1")
                {
                    var column=w.Objects.Find(o=>o.objectId.StartsWith("White central column") && Mathf.Abs(o.Bounds.yMax-11.05f)<.001f);
                    if(!column || Mathf.Abs(column.Bounds.xMin-definition.terrain.Find(t=>t.id=="Recess bottom").rect.xMin)>.001f) throw new Exception("Central white column does not match reference");
                    var main=w.Objects.Find(o=>o.objectId.StartsWith("White main region") && Mathf.Abs(o.Bounds.yMax-(2*definition.mirrorAxis-definition.terrain.Find(t=>t.id=="Main blue plateau").rect.yMax-Rules.BoxSize-2*Rules.Skin))<.001f);
                    if(!main) throw new Exception("Main white boundary does not support the boxes");
                }
                if(!w.Mirror.TryInspectionTransfer(w.Zones[0])) throw new Exception(w.Failure);
                foreach(var o in w.Objects) if(o.kind!=ObjectKind.Player && Vector2.Distance(o.Position,source[o])>.00001f) throw new Exception("Second mirror did not restore "+o.objectId);
                if(Vector3.Distance(w.Goal.position,root)>.00001f || w.Goal.localScale!=scale || Vector3.Distance(pole.position,poleBefore)>.00001f) throw new Exception("Flag did not restore after two mirrors");
                w.ResetLevel(); if(w.Goal.localScale!=scale) throw new Exception("Reset lost flag orientation");
                result="PASS "+definition.levelId+" first reflection: exact terrain/box centers, matching visible white solids, complete flag+pole reflection.\nPASS second reflection restores geometry and complete flag.\nPASS reset restores flag scale.\n";
            }
            catch(Exception e) { result="FAIL "+e; throw; }
            finally
            {
                Directory.CreateDirectory("DevReports"); File.WriteAllText("DevReports/RealmCorrection-"+definition.levelId+".txt",result);
                SceneManager.SetActiveScene(previous); EditorSceneManager.CloseScene(scene,true);
            }
        }
    }
}


