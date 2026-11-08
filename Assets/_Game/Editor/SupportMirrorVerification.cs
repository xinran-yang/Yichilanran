using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace YiChiLanRan.Editor
{
    public static class SupportMirrorVerification
    {
        static LevelRuntime world;
        static TransferBox2D box;
        static PhaseObject platform;
        static int stage,cycles;
        static float until;
        static double started;
        static bool oldEnabled;
        static EnterPlayModeOptions oldOptions;
        static Vector2 boxBefore,platformBefore;
        static readonly StringBuilder report=new StringBuilder();
        static readonly StringBuilder csv=new StringBuilder();
        static void Require(bool ok,string message)
        { report.AppendLine((ok ? "PASS ":"FAIL ")+message); if(!ok) throw new Exception(message); }
        public static void BuildAndRun()
        {
            GreyboxBuilder.MirrorInspectionScene();
            EditorSceneManager.OpenScene(GreyboxBuilder.Root+"Scenes/MirrorInspection.unity");
            report.Clear(); report.AppendLine("Unity "+Application.unityVersion+" minimal one-platform support mirror test (actual Play Mode)");
            csv.Clear(); csv.AppendLine("cycle,axis,boxBeforeX,boxBeforeY,boxAfterX,boxAfterY,platformBeforeY,platformAfterY,playerColor,boxColor,simulated,gravityScale,customGravityY,support,side,heldYError");
            oldEnabled=EditorSettings.enterPlayModeOptionsEnabled; oldOptions=EditorSettings.enterPlayModeOptions;
            EditorSettings.enterPlayModeOptionsEnabled=true; EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;
            stage=-1;cycles=0; started=EditorApplication.timeSinceStartup;
            EditorApplication.update+=Tick; Application.logMessageReceived+=Log;
        }
        static void Log(string text,string stack,LogType type)
        {
            if(stack.Contains("UnityEditor.Search.SearchDatabase") && !stack.Contains("YiChiLanRan")) { report.AppendLine("EDITOR INFRASTRUCTURE "+text); return; }
            if(type==LogType.Exception || type==LogType.Error || type==LogType.Assert) Finish(false,text+"\n"+stack);
        }
        static void Tick()
        {
            try
            {
                if(EditorApplication.timeSinceStartup-started>45) throw new Exception("Timeout stage="+stage);
                if(stage==-1) { if(EditorApplication.timeSinceStartup-started>1) { stage=0; EditorApplication.EnterPlaymode(); } return; }
                if(!EditorApplication.isPlaying) return;
                if(!world || !box || !platform)
                {
                    world=UnityEngine.Object.FindFirstObjectByType<LevelRuntime>(); if(!world || world.Clock<.15f) return;
                    box=world.GetComponentInChildren<TransferBox2D>(); platform=world.Objects.Find(o=>o.objectId=="Reference platform");
                    Require(world.Objects.FindAll(o=>o.kind==ObjectKind.Terrain).Count==1 && world.Objects.FindAll(o=>o.kind==ObjectKind.Box).Count==1 && world.Zones.Count==1,"Exactly one platform, one box and one yellow zone");
                    Require(box.HasReferenceSupport,"Original platform contact captured by identity");
                }
                if(stage==0)
                {
                    boxBefore=box.Entity.Position; platformBefore=platform.Position;
                    Require(world.Mirror.TryInspectionTransfer(world.Zones[0]),"Yellow inspection transfer accepted"); stage=1;
                }
                else if(stage==1 && !world.Busy)
                {
                    float axis=world.definition.mirrorAxis;
                    Require(Vector2.Distance(box.Entity.Position,Rules.Mirror(boxBefore,axis))<.00001f,"Box center uses exact mirror formula");
                    Require(Vector2.Distance(platform.Position,Rules.Mirror(platformBefore,axis))<.00001f,"Platform center uses same axis");
                    Require(box.Entity.phase==Phase.Blue,"Yellow keeps box color");
                    Require(box.referenceSupportId==platform.objectId && box.referenceSupportSide==(world.Mirrored ? -1:1),"Original support identity and reflected side retained");
                    Require(box.Entity.Body.simulated==(world.Player.Entity.phase==Phase.Blue),"Inactive box suspends physics; matching box is simulated");
                    Require((box.Entity.Body.constraints&RigidbodyConstraints2D.FreezePositionY)!=0,"Supported vertical coordinate is constrained");
                    Require(box.Entity.Body.gravityScale==0 && box.CustomGravityY(world)==0 && Physics2D.gravity.y<0,"No global gravity inversion; supported box gets no vertical acceleration");
                    boxBefore=box.Entity.Position; platformBefore=platform.Position; until=world.Clock+.45f; stage=2;
                }
                else if(stage==2 && world.Clock>=until)
                {
                    float error=Mathf.Abs(box.Entity.Position.y-boxBefore.y);
                    Require(error<.00001f,"No box drift/fall across normal physics frames");
                    var b=box.Entity; var ci=System.Globalization.CultureInfo.InvariantCulture;
                    csv.Append(cycles).Append(',').Append(world.definition.mirrorAxis).Append(',').Append(b.Position.x.ToString("R",ci)).Append(',').Append((2*world.definition.mirrorAxis-b.Position.y).ToString("R",ci)).Append(',').Append(b.Position.x.ToString("R",ci)).Append(',').Append(b.Position.y.ToString("R",ci)).Append(',').Append((2*world.definition.mirrorAxis-platform.Position.y).ToString("R",ci)).Append(',').Append(platform.Position.y.ToString("R",ci)).Append(',').Append(world.Player.Entity.phase).Append(',').Append(b.phase).Append(',').Append(b.Body.simulated).Append(',').Append(b.Body.gravityScale).Append(',').Append(box.CustomGravityY(world)).Append(',').Append(box.referenceSupportId).Append(',').Append(box.referenceSupportSide).Append(',').Append(error.ToString("R",ci)).AppendLine();
                    cycles++;
                    if(cycles<5) stage=0;
                    else
                    {
                        // Inspection fixture: activate the blue box while it is UNDER its original blue platform.
                        world.Player.Entity.SetPhase(Phase.Blue); box.ApplyPhaseState(world); PhaseCollisionSystem.Refresh(world);
                        Require(world.Mirrored && box.Entity.Body.simulated && box.referenceSupportSide==-1,"Matching-color underside support is active");
                        boxBefore=box.Entity.Position; until=world.Clock+.4f; box.inspectionPush=1;
                        // The inspector component reads the physical keyboard; disable it while automated fixture input runs.
                        world.GetComponent<MirrorInspection>().enabled=false; stage=3;
                    }
                }
                else if(stage==3 && world.Clock>=until)
                {
                    box.inspectionPush=0;
                    Require(box.Entity.Position.x>boxBefore.x+1,"Active underside box moves horizontally through Rigidbody2D");
                    Require(Mathf.Abs(box.Entity.Position.y-boxBefore.y)<.00001f,"Horizontal underside push does not change vertical support relation");
                    Require(box.referenceSupportId==platform.objectId,"Push keeps original support, not a nearest-platform replacement");
                    world.ResetLevel(); Require(!world.Mirrored && world.Transfers==0 && box.referenceSupportSide==1,"Reset restores initial world and support side");
                    report.AppendLine("LIMIT Inspection push is a direct debug input; player-side pushing in redesigned maps is not certified by this minimal test.");
                    Finish(true,"");
                }
            }
            catch(Exception e) { Finish(false,e.ToString()); }
        }
        static void Finish(bool ok,string reason)
        {
            EditorApplication.update-=Tick; Application.logMessageReceived-=Log;
            EditorSettings.enterPlayModeOptionsEnabled=oldEnabled; EditorSettings.enterPlayModeOptions=oldOptions;
            if(!ok) report.AppendLine("FAIL "+reason);
            Directory.CreateDirectory("DevReports"); File.WriteAllText("DevReports/SupportMirror-Minimal.txt",report.ToString()); File.WriteAllText("DevReports/SupportMirror-Coordinates.csv",csv.ToString());
            EditorApplication.Exit(ok ? 0:1);
        }
    }
}

