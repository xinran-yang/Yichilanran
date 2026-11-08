using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace YiChiLanRan.Editor
{
    public static class Level22RevisionChecks
    {
        public static void BuildAndRun()
        {
            LevelCatalog.Generate22();
            File.WriteAllText("DevReports/Reference-2.2.json",JsonUtility.ToJson(Level22Layout.Create(),true));
            Geometry(); BlueJunction(); Box3Ceiling(); Route();
        }
        static void Geometry()
        {
            var previous=SceneManager.GetActiveScene();
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            var report=new StringBuilder("Level 2.2 only: geometry transaction fixture, no physical integration.\n");
            try
            {
                SceneManager.SetActiveScene(scene); var d=Level22Layout.Create();
                var w=GreyboxBuilder.Create(d); w.Initialize(true);
                var poses=new Dictionary<PhaseObject,Vector2>(); foreach(var o in w.Objects) poses[o]=o.Position;
                var flag=w.Goal.position; var flagScale=w.Goal.localScale;
                var pole=w.Goal.Find("Flag pole"); var polePosition=pole.position;
                var pad=w.Zones[0]; w.Player.Entity.Place(new Vector2(11.4f,pad.Floor+Rules.PlayerHeight/2+Rules.Skin));
                d.mirrorInspection=true;
                if(!w.Mirror.TryInspectionTransfer(pad)) throw new Exception(w.Failure);
                report.AppendLine("axis="+d.mirrorAxis);
                foreach(var o in w.Objects)
                {
                    if(o.kind==ObjectKind.Player) continue;
                    var expected=Rules.Mirror(poses[o],d.mirrorAxis);
                    if(Vector2.Distance(expected,o.Position)>.00001f) throw new Exception("Wrong reflected center: "+o.objectId);
                    if(o.kind==ObjectKind.Terrain) report.AppendLine(o.objectId+" before="+poses[o].ToString("F4")+" expected="+expected.ToString("F4")+" actual="+o.Position.ToString("F4")+" phase="+o.phase+" collider="+o.Shape.enabled);
                    if(o.kind==ObjectKind.Box)
                    {
                        var box=o.GetComponent<TransferBox2D>();
                        report.AppendLine(o.objectId+" before="+poses[o].ToString("F4")+" expected="+expected.ToString("F4")+" actual="+o.Position.ToString("F4")+" phase="+o.phase+" gravityScale="+o.Body.gravityScale+" customGravity="+box.CustomGravityY(w)+" simulated="+o.Body.simulated+" collider="+o.Shape.enabled+" support="+box.referenceSupportId+" side="+box.referenceSupportSide);
                    }
                }
                if(Vector2.Distance(pole.position,Rules.Mirror(polePosition,d.mirrorAxis))>.00001f || w.Goal.localScale.y!=-flagScale.y) throw new Exception("Complete flag reflection failed");
                if(!w.Objects.Exists(o=>o.objectId=="White upper yellow shelf" && Mathf.Abs(o.Bounds.yMax-10.95f)<.001f)) throw new Exception("Missing upper white silhouette");
                var middle=w.Objects.Find(o=>o.objectId.StartsWith("White middle region") && Mathf.Abs(o.Bounds.yMax-4.86f)<.001f);
                var right=w.Objects.Find(o=>o.objectId.StartsWith("White right region") && Mathf.Abs(o.Bounds.yMax-6.86f)<.001f);
                if(!middle || !right || Mathf.Abs(middle.Bounds.xMax-right.Bounds.xMin)>.00001f || Mathf.Abs(middle.Bounds.xMax-Level22Layout.WhiteRightJunction)>.00001f) throw new Exception("White roads do not share the required junction");
                report.AppendLine("PASS extended white road meets Green 2 platform exactly at x="+middle.Bounds.xMax+", no horizontal gap; height difference="+(right.Bounds.yMax-middle.Bounds.yMax));
                var blueMiddle=d.terrain.Find(t=>t.id=="Central middle body").rect;
                var blueRight=d.terrain.Find(t=>t.id=="Right lower arm").rect;
                var crown=d.terrain.Find(t=>t.id=="Box 2 pit counterpart crown").rect;
                var whitePit=w.Objects.Find(o=>o.objectId.StartsWith("White box 2 pit"));
                float clearance=Rules.BoxSize+2*Rules.Skin;
                if(!whitePit || Mathf.Abs(blueMiddle.xMax-middle.Bounds.xMax)>.00001f || Mathf.Abs(blueRight.xMin-(right.Bounds.xMin-Level22Layout.BlueConnectionOverlap))>.00001f || Mathf.Abs(crown.xMin-whitePit.Bounds.xMin)>.00001f || Mathf.Abs(crown.xMax-whitePit.Bounds.xMax)>.00001f || Mathf.Abs(whitePit.Bounds.yMax-(2*d.mirrorAxis-crown.yMax-clearance))>.00001f) throw new Exception("Blue/white profile counterparts are inconsistent");
                float overlapX=Mathf.Min(blueMiddle.xMax,blueRight.xMax)-Mathf.Max(blueMiddle.xMin,blueRight.xMin);
                float overlapY=Mathf.Min(blueMiddle.yMax,blueRight.yMax)-Mathf.Max(blueMiddle.yMin,blueRight.yMin);
                float notchHeight=d.terrain.Find(t=>t.id=="Green 1 underside notch").rect.height;
                if(overlapX<.399f || overlapY<1.399f || notchHeight<1.399f) throw new Exception("Blue connection / Green 1 band is too narrow");
                report.AppendLine("PASS blue connection volume="+overlapX+" x "+overlapY+"; Green 1 minimum blue band height="+notchHeight+"; box height="+Rules.BoxSize);
                var pillar=d.terrain.Find(t=>t.id=="Blue hanging pillar shell");
                float underPillar=pillar.rect.yMin-blueMiddle.yMax;
                var mirroredPillar=w.Objects.Find(o=>o.objectId==pillar.id);
                var targetPillar=Rules.Mirror(pillar.rect,d.mirrorAxis);
                if(pillar.phase!=Phase.Blue || Mathf.Abs(underPillar)>.00001f || Mathf.Abs(pillar.rect.xMin-blueMiddle.xMax)>.00001f || Mathf.Abs(mirroredPillar.Bounds.yMax-targetPillar.yMax)>.00001f || Mathf.Abs(mirroredPillar.Bounds.xMin-pillar.rect.xMin)>.00001f) throw new Exception("Pillar corner contact / white reflection incorrect");
                report.AppendLine("PASS blue hanging shell="+pillar.rect+", overhead gap="+underPillar+"; white-state blue shell="+targetPillar+"; opposite-phase filtering preserved.");
                report.AppendLine("PASS single corner contact at "+new Vector2(pillar.rect.xMin,pillar.rect.yMin)+"; no pillar ceiling above main-platform pushing area.");
                var flagRect=d.terrain.Find(t=>t.id=="Right upper flag ledge").rect;
                float box3Top=d.boxes.Find(o=>o.id=="Box 3").center.y+Rules.BoxSize/2;
                float gap=flagRect.yMin-box3Top;
                if(gap>=Rules.PlayerHeight-.1f || gap<=0) throw new Exception("Box 3 overhead gap does not block a standing character");
                var hole=Rules.Mirror(flagRect,d.mirrorAxis);
                foreach(var o in w.Objects) if(o.kind==ObjectKind.Terrain && o.phase==Phase.White && Rules.Overlap(o.Bounds,hole,.001f)) throw new Exception("White geometry did not reflect the new flag ledge cutout");
                report.AppendLine("PASS box 3 overhead gap="+gap+" < player height="+Rules.PlayerHeight+"; exact white island cutout="+hole);
                report.AppendLine("PASS blue crown/pit share x="+crown.xMin+".."+crown.xMax+"; blue main/right junction="+blueMiddle.xMax+"; white pit derives from blue top="+crown.yMax+", clearance="+clearance);
                report.AppendLine("PASS exact shared-axis terrain/box centers, complete flag pole and upper white silhouette.");
                if(!w.Mirror.TryInspectionTransfer(pad)) throw new Exception(w.Failure);
                foreach(var o in w.Objects) if(o.kind!=ObjectKind.Player && Vector2.Distance(o.Position,poses[o])>.00001f) throw new Exception("Double reflection failed: "+o.objectId);
                if(w.Goal.localScale!=flagScale || Vector3.Distance(w.Goal.position,flag)>.00001f || Vector3.Distance(pole.position,polePosition)>.00001f) throw new Exception("Flag did not restore");
                report.AppendLine("PASS second reflection restores all terrain/boxes and full flag.");
            }
            catch(Exception e) {report.AppendLine("FAIL "+e); throw;}
            finally {File.WriteAllText("DevReports/Level22Revision-Geometry.txt",report.ToString()); SceneManager.SetActiveScene(previous); EditorSceneManager.CloseScene(scene,true);}
        }
        static void BlueJunction()
        {
            var previous=SceneManager.GetActiveScene();
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            var old=Physics2D.simulationMode; Physics2D.simulationMode=SimulationMode2D.Script;
            string result=""; RouteDriver b=null;
            try
            {
                SceneManager.SetActiveScene(scene); var w=GreyboxBuilder.Create(Level22Layout.Create()); w.Initialize(true);
                // Local shortcut attempt starts LEFT of the new shell, never inside it.
                w.Player.Entity.Place(new Vector2(15.5f,Level22Layout.BlueMainTop+Rules.PlayerHeight/2+Rules.Skin));
                b=new RouteDriver(w,scene.GetPhysicsScene2D()); b.Wait(.3f); float farthest=w.Player.Entity.Position.x;
                for(int attempt=0;attempt<3;attempt++)
                {
                    for(int i=0;i<360;i++)
                    {
                        b.Tick(1,attempt>0 && w.Player.Grounded,attempt==2 && !w.Player.Grounded && w.Player.Entity.Body.linearVelocity.y<0);
                        farthest=Mathf.Max(farthest,w.Player.Entity.Position.x);
                        if(w.Player.Entity.Position.x>Level22Layout.HangingPillarLeft-Rules.PlayerWidth/2+.03f || w.Transfers!=0 || w.Complete) throw new Exception("Blue shortcut crossed pillar: "+b.State());
                    }
                }
                result="PASS BLUE local shortcut attempts: walk, jump, jump+dive all blocked LEFT of pillar.\nfarthest center x="+farthest+"; pillar left="+Level22Layout.HangingPillarLeft+"; gap="+(Level22Layout.HangingPillarBottom-Level22Layout.BlueMainTop)+"; player height="+Rules.PlayerHeight+"\n"+b.State();
            }
            catch(Exception e) {result="FAIL "+e; throw;}
            finally
            {
                File.WriteAllText("DevReports/Level22Revision-BlueJunction.txt",result); b?.Save("DevReports/Level22Revision-BlueJunction.csv");
                Physics2D.simulationMode=old; SceneManager.SetActiveScene(previous); EditorSceneManager.CloseScene(scene,true);
            }
        }
        static void Box3Ceiling()
        {
            var previous=SceneManager.GetActiveScene();
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            var old=Physics2D.simulationMode; Physics2D.simulationMode=SimulationMode2D.Script;
            string result=""; RouteDriver b=null;
            try
            {
                SceneManager.SetActiveScene(scene); var w=GreyboxBuilder.Create(Level22Layout.Create()); w.Initialize(true);
                // Stronger local fixture: an already-BLUE, solid box 3 at its original position.
                // This is not a legal route state; it checks the ceiling independently of phase filtering.
                var box=w.Objects.Find(o=>o.objectId=="Box 3"); box.SetPhase(Phase.Blue);
                box.GetComponent<TransferBox2D>().ApplyPhaseState(w); PhaseCollisionSystem.Refresh(w);
                w.Player.Entity.Place(new Vector2(20.35f,Level22Layout.BlueRightTop+Rules.PlayerHeight/2+Rules.Skin));
                b=new RouteDriver(w,scene.GetPhysicsScene2D()); b.Wait(.3f); float highest=w.Player.Entity.Bounds.yMin;
                for(int i=0;i<480;i++)
                {
                    b.Tick(1,w.Player.Grounded); highest=Mathf.Max(highest,w.Player.Entity.Bounds.yMin);
                    if(w.Complete || w.Player.Entity.Bounds.yMin>=box.Bounds.yMax-.04f) throw new Exception("Character climbed box 3 under the ceiling");
                }
                result="PASS explicit BLUE box/player fixture: repeated rightward jump attempts under flag ledge blocked.\nmax feet="+highest+"; box top="+box.Bounds.yMax+"; ceiling="+Level22Layout.FlagLedgeBottom+"; player height="+Rules.PlayerHeight;
            }
            catch(Exception e) {result="FAIL "+e; throw;}
            finally
            {
                File.WriteAllText("DevReports/Level22Revision-Box3Ceiling.txt",result); b?.Save("DevReports/Level22Revision-Box3Ceiling.csv");
                Physics2D.simulationMode=old; SceneManager.SetActiveScene(previous); EditorSceneManager.CloseScene(scene,true);
            }
        }
        static void Dive(RouteDriver b,float x,float delay=.24f)
        {
            b.Walk(x); b.Wait(.15f); int n=b.world.Transfers;
            b.Tick(0,true); for(int i=0;i<delay/Rules.Step;i++) b.Tick(); b.Tick(0,false,true);
            for(int i=0;i<260;i++) {b.Tick(); if(b.world.Transfers>n) {b.Wait(1); return;}}
            throw new Exception("Dive failed: "+b.State());
        }
        public static string FollowRoute(RouteDriver b)
        {
            var w=b.world; var report=new StringBuilder();
            b.Wait(.4f); b.Jump(3.9f); b.Walk(8.5f); b.Wait(.3f); b.Walk(11.4f); Dive(b,11.4f);
            // Walk off the white yellow island into the central corridor, then approach box 2 from its left.
            b.Walk(13.65f); b.Wait(1.1f); b.Jump(11.1f); b.Jump(9.8f); b.Jump(8.7f);
            var box2=w.Objects.Find(o=>o.objectId=="Box 2"); float beforeX=box2.Position.x,beforeY=box2.Position.y;
            b.Walk(11.96f); b.Wait(.7f);
            report.AppendLine("Box 2 pushed into pit: "+new Vector2(beforeX,beforeY).ToString("F4")+" -> "+box2.Position.ToString("F4"));
            if(box2.Position.x<12.68f || box2.Position.x>12.8f || Mathf.Abs(box2.Bounds.yMax-4.88f)>.05f) throw new Exception("Box 2 did not physically fill the new pit");
            Dive(b,6.5f,.25f);
            b.Jump(4.8f); b.Jump(2.7f); b.Walk(5.6f); b.Wait(.7f);
            float x=w.Objects.Find(o=>o.objectId=="Box 1").Position.x; b.Jump(x); Dive(b,x);
            b.Walk(x-.9f); b.Wait(.4f); b.Walk(15.12f,10); b.Jump(15.9f); b.Jump(17.3f);
            b.Walk(20.45f); b.Jump(21.3f); b.Jump(22.3f); b.Wait(.3f); b.Walk(19.78f);
            x=w.Objects.Find(o=>o.objectId=="Box 3").Position.x; b.Jump(x); Dive(b,x); b.Jump(20.6f); b.Walk(22.1f);
            var parkedBox1=w.Objects.Find(o=>o.objectId=="Box 1"); var shell=w.Objects.Find(o=>o.objectId=="Blue hanging pillar shell");
            if(Rules.Overlap(parkedBox1.Bounds,shell.Bounds,.025f)) throw new Exception("Parked box 1 overlaps blue pillar after final mirror");
            report.AppendLine("PASS parked box 1 after final BLUE mirror: "+parkedBox1.Bounds+"; pillar="+shell.Bounds+"; no penetration beyond 0.025 contact tolerance.");
            if(!w.Complete || string.Join("|",w.EventHistory)!="Yellow solo|Green 1 solo|Green 1 + Box 1|Green 2 + Box 3") throw new Exception("Incorrect route completion: "+b.State());
            report.AppendLine("PASS yellow -> push box 2 -> green 1 solo -> bring box 1 -> push box 1 across box 2 -> green 2 with box 3 -> flag.");
            report.AppendLine(b.State());
            return report.ToString();
        }
        static void Route()
        {
            var previous=SceneManager.GetActiveScene();
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            var old=Physics2D.simulationMode; Physics2D.simulationMode=SimulationMode2D.Script;
            var report=new StringBuilder("Unity PhysicsScene2D continuous input route; no mid-route placement.\n"); RouteDriver b=null;
            try
            {
                SceneManager.SetActiveScene(scene); var w=GreyboxBuilder.Create(Level22Layout.Create()); w.Initialize(true);
                b=new RouteDriver(w,scene.GetPhysicsScene2D());
                report.Append(FollowRoute(b));
            }
            catch(Exception e) {report.AppendLine("FAIL "+e); throw;}
            finally
            {
                File.WriteAllText("DevReports/Level22Revision-Route.txt",report.ToString()); b?.Save("DevReports/Level22Revision-Route.csv");
                Physics2D.simulationMode=old; SceneManager.SetActiveScene(previous); EditorSceneManager.CloseScene(scene,true);
            }
        }
    }
}

