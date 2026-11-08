using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace YiChiLanRan.EditorTools
{
    [InitializeOnLoad]
    public static class Chapter02Smoke
    {
        static Chapter02Smoke()
        {
            EditorApplication.playModeStateChanged+=state=>
            {
                if(state!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("Chapter02Smoke",false)) return;
                new GameObject("Editor only · original Level01 route").AddComponent<Chapter01SmokeDriver>();
                new GameObject("Editor only · continuous Level02 route").AddComponent<Chapter02SmokeDriver>();
            };
        }
        [MenuItem("YiChiLanRan/Validate Continuous Levels Once %#F9")]
        public static void Run()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) return;
            SessionState.SetBool("Chapter01Smoke",false); SessionState.SetBool("Chapter01Preview",false);
            SessionState.SetBool("Chapter02Smoke",true);
            Application.runInBackground=true;
            EditorApplication.isPlaying=true;
        }
    }
    [DefaultExecutionOrder(-90)]
    public sealed class Chapter02SmokeDriver : MonoBehaviour
    {
        private ChapterWorld world;
        private ClothPlayer actor;
        private int stage,sceneHandle,actorId;
        private float started,jumpAt;
        private bool checkpointTested;
        private readonly System.Text.StringBuilder evidence=new System.Text.StringBuilder();
        private void Start()
        {
            world=FindFirstObjectByType<ChapterWorld>(); actor=world.player;
            sceneHandle=SceneManager.GetActiveScene().handle; actorId=actor.GetInstanceID(); started=Time.time;
            Directory.CreateDirectory("artifacts");
            var p=world.Parameters;
            Require(world.gates.Length==4,"one original gate, one connection, two Level02 gates");
            Require(600-230>p.DiveBounceHeight*p.PixelsPerUnit,"A necessity: 370px wall > maximum 222.893px rebound");
            Require(620-200>p.RigidJumpHeight*p.PixelsPerUnit,"B necessity: 420px white exit wall > white jump");
            Require(3950-3040>2*p.RigidJumpSpeed/(-p.GravityY)*p.HorizontalSpeed*p.PixelsPerUnit+28,"B white gap > same-height jump plus body width");
            ValidateGeometry();
        }
        private void Require(bool condition,string message)
        {
            if(!condition) throw new InvalidOperationException(message);
            evidence.AppendLine(message);
        }
        private void ValidateGeometry()
        {
            foreach(var piece in world.terrain)
            {
                Vector2 expected=world.Mirrored?ChapterWorld.Reflect(piece.baseCenter,world.Axis):piece.baseCenter;
                Require(Vector2.Distance(piece.transform.position,expected)<.00001f,"geometry "+piece.name);
                Require(Vector2.Distance(piece.Shape.size,piece.size)<.00001f,"collider "+piece.name);
                Require(Vector2.Distance(ChapterWorld.Reflect(ChapterWorld.Reflect(piece.baseCenter,world.Axis),world.Axis),piece.baseCenter)<.00001f,"involution "+piece.name);
            }
        }
        private bool JumpForStep(float direction)
        {
            if(!actor.Grounded) return false;
            float x=actor.Foot.x,foot=actor.Foot.y;
            foreach(var piece in world.terrain)
            {
                if(!piece.Shape.enabled) continue;
                var r=piece.WorldRect;
                if(r.yMax<=foot+.01f) continue;
                if(direction>0 && r.xMin>x && r.xMin-x<.48f) return true;
            }
            return false;
        }
        private void FixedUpdate()
        {
            if(!actor || !world.Ready) return;
            try
            {
                if(Time.time-started>110) throw new Exception("timeout stage="+stage+" foot="+actor.Foot);
                if(SceneManager.GetActiveScene().handle!=sceneHandle || actor.GetInstanceID()!=actorId) throw new Exception("Scene/player replaced");
                float x=actor.Foot.x; float move=0; bool jump=false,dive=false;
                switch(stage)
                {
                    case 0: if(!world.Complete) return; stage=1; evidence.AppendLine("original Level01 completed continuously"); break;
                    case 1: move=x<17.28f?1:0; if(x>=17.28f && actor.Grounded){jump=true;jumpAt=Time.time;stage=2;} break;
                    case 2: if(Time.time-jumpAt>.18f){dive=true;stage=3;} break;
                    case 3: if(!world.Mirrored){Require(world.CrossingCount==2,"connection white -> blue");stage=4;} break;
                    case 4:
                        move=x<20.38f?1:0;
                        if(x>=18.6f && actor.Grounded && !checkpointTested)
                        {
                            Require(world.CheckpointIndex==1 && !world.RecoveryWhite,"connection checkpoint blue");
                            world.Respawn(); Require(Vector2.Distance(actor.Foot,world.RecoveryFoot)<.00001f && actor.Blue,"checkpoint restores foot and colour");checkpointTested=true;
                        }
                        if(x>=20.38f && actor.Grounded){jump=true;jumpAt=Time.time;stage=5;} break;
                    case 5: if(Time.time-jumpAt>.18f){dive=true;stage=6;} break;
                    case 6: if(world.Mirrored){Require(world.CrossingCount==3,"A blue -> white");ValidateGeometry();Capture("artifacts/level02-A.png");stage=7;} break;
                    case 7: move=1; if(x>=22.03f && actor.Grounded){jump=true;stage=8;} break;
                    case 8:
                        move=x<28.78f?1:0; jump=JumpForStep(1);
                        if(x>=28.78f && actor.Grounded){move=0;jump=true;jumpAt=Time.time;stage=9;} break;
                    case 9: if(Time.time-jumpAt>.18f){dive=true;stage=10;} break;
                    case 10: if(!world.Mirrored){Require(world.CrossingCount==4,"B white -> blue");ValidateGeometry();Capture("artifacts/level02-B.png");stage=11;} break;
                    case 11:
                        move=1; jump=JumpForStep(1);
                        if(world.Level02Complete)
                        {
                            Require(world.CheckpointIndex==2,"upper blue checkpoint reached");
                            Require(!world.showDebugOverlay,"no formal text overlay");
                            Capture("artifacts/level02-complete.png"); Finish(true,"full continuous route reached Level02 exit");return;
                        }
                        break;
                }
                actor.SetInputForValidation(move,jump,dive);
            }
            catch(Exception e){Finish(false,e.Message);}
        }
        private static void Capture(string path) { if(!Application.isBatchMode) ScreenCapture.CaptureScreenshot(path); }
        private void Finish(bool passed,string message)
        {
            File.WriteAllText("artifacts/level02-smoke.txt",(passed?"PASS":"FAIL")+"\n"+message+"\nstage="+stage+" foot="+actor.Foot+" crossings="+world.CrossingCount+"\n"+evidence);
            SessionState.SetBool("Chapter02Smoke",false);actor.ReleaseValidationInput();
            if(passed)Debug.Log("CHAPTER02_SMOKE_PASS "+message);else Debug.LogError("CHAPTER02_SMOKE_FAIL "+message);
            enabled=false;
            EditorApplication.delayCall+=()=>
            {
                EditorApplication.isPlaying=false;
                if(Application.isBatchMode)
                {
                    EditorApplication.delayCall+=()=>
                    {
                        bool built=passed && Chapter02Builder.BuildWindowsPlayer();
                        EditorApplication.Exit(passed && built?0:1);
                    };
                }
            };
        }
    }
}
