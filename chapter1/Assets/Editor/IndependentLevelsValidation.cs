using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace YiChiLanRan.EditorTools
{
    [InitializeOnLoad]
    public static class IndependentLevelsValidation
    {
        static IndependentLevelsValidation()
        {
            EditorApplication.playModeStateChanged+=state=>
            {
                if(state!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("IndependentLevelsValidation",false))return;
                new GameObject("Editor only · unchanged first route").AddComponent<Chapter01SmokeDriver>();
                var obj=new GameObject("Editor only · transitions and layered route");UnityEngine.Object.DontDestroyOnLoad(obj);obj.AddComponent<IndependentRouteDriver>();
            };
        }
        [MenuItem("YiChiLanRan/Validate Independent Levels Once")]
        public static void Run()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)return;
            EditorSceneManager.OpenScene(IndependentLevelsBuilder.FirstScene);
            SessionState.SetBool("Chapter01Smoke",false);SessionState.SetBool("Chapter01Preview",false);SessionState.SetBool("Chapter02Smoke",false);
            SessionState.SetBool("IndependentLevelsValidation",true);Application.runInBackground=true;EditorApplication.isPlaying=true;
        }
    }
    [DefaultExecutionOrder(-90)]
    public sealed class IndependentRouteDriver : MonoBehaviour
    {
        public bool continueToThird;
        private ChapterWorld world;private ClothPlayer actor;
        private int stage,completedScene;private float started,phaseTime;
        private bool sawLock,sawNewLock,hadError;
        private readonly System.Text.StringBuilder report=new System.Text.StringBuilder();
        private void Start(){started=Time.realtimeSinceStartup;Directory.CreateDirectory("artifacts");Application.logMessageReceived+=OnLog;}
        private void OnLog(string text,string stack,LogType type){if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert)hadError=true;}
        private void Check(bool condition,string message){if(!condition)throw new InvalidOperationException(message);report.AppendLine(message);}
        private void Audit(string label)
        {
            Check(actor.transform.rotation==Quaternion.identity && actor.transform.localScale==Vector3.one,label+": upright and scale");
            foreach(var piece in world.terrain)
            {
                var expected=world.Mirrored?ChapterWorld.Reflect(piece.baseCenter,world.Axis):piece.baseCenter;
                Check(Vector2.Distance(piece.transform.position,expected)<.00001f && Vector2.Distance(piece.Shape.size,piece.size)<.00001f,label+": visual/collider "+piece.name);
                Check(Vector2.Distance(ChapterWorld.Reflect(ChapterWorld.Reflect(piece.baseCenter,world.Axis),world.Axis),piece.baseCenter)<.00001f,label+": double reflection "+piece.name);
            }
            File.WriteAllText("artifacts/rework-state-"+label+".json",JsonUtility.ToJson(new State{phase=label,foot=actor.Foot,axis=world.Axis,blue=actor.Blue,crossings=world.CrossingCount},true));
        }
        [Serializable]private sealed class State{public string phase;public Vector2 foot;public float axis;public bool blue;public int crossings;}
        private bool JumpForStep(float direction)
        {
            if(!actor.Grounded)return false;
            float height=actor.Blue?world.Parameters.ElasticJumpHeight:world.Parameters.RigidJumpHeight;
            foreach(var piece in world.terrain)
            {
                if(!piece.Shape.enabled)continue;var r=piece.WorldRect;float rise=r.yMax-actor.Foot.y;
                if(rise<=.01f || rise>height+.01f)continue;
                if(direction>0 && r.xMin>actor.Foot.x && r.xMin-actor.Foot.x<.44f)return true;
                if(direction<0 && r.xMax<actor.Foot.x && actor.Foot.x-r.xMax<.44f)return true;
            }
            return false;
        }
        private void FixedUpdate()
        {
            try
            {
                if(Time.realtimeSinceStartup-started>100)throw new Exception("Timeout at stage "+stage);
                if(hadError)throw new Exception("Unity logged an error/assertion");
                world=FindFirstObjectByType<ChapterWorld>();if(!world)return;actor=world.player;
                var manager=LevelTransitionManager.Instance;
                if(stage==0)
                {
                    if(manager && manager.IsTransitioning)
                    {
                        Check(actor.ControlsLocked && actor.Velocity==Vector2.zero,"transition input locked, velocity zero");sawLock=true;
                        if(world.Data.levelId=="level02")sawNewLock=true;
                    }
                    if(world.Data.levelId!="level02" || !world.SpawnInitialized || !manager || manager.IsTransitioning)return;
                    Check(sawLock && sawNewLock && manager.CompletedTransitions==1 && manager.LoadedUnderBlack && manager.Alpha==0,"one completed fully-black scene load and fade-in");
                    Check(actor.Blue && Vector2.Distance(actor.Foot,new Vector2(1,-5.5f))<.0001f && actor.Velocity==Vector2.zero,"independent blue spawn reset");
                    Check(world.gates.Length==2 && world.terrain.Length==30,"layered complementary geometry and two shared gates");
                    completedScene=SceneManager.GetActiveScene().handle;Audit("initial");stage=1;phaseTime=Time.time;
                    report.AppendLine(LayeredReachability.Analyze(world));
                }
                if(stage>0)Check(SceneManager.GetActiveScene().handle==completedScene,"second level remains same scene");
                float x=actor.Foot.x,move=0;bool jump=false,dive=false;
                switch(stage)
                {
                    case 1:
                        move=1;
                        if(x>6.7f){jump=actor.Grounded;dive=!actor.Grounded && actor.Velocity.y<=0;}
                        if(Time.time-phaseTime>4)
                        {
                            Check(x<=6.861f && world.CrossingCount==0,"initial lower chamber cannot reach upper route by jump/rebound");stage=2;
                        }
                        break;
                    case 2:move=x>2.80f?-1:0;if(x<=2.80f && actor.Grounded){move=0;jump=true;phaseTime=Time.time;stage=3;}break;
                    case 3:if(Time.time-phaseTime>.16f){dive=true;stage=4;}break;
                    case 4:if(world.Mirrored){Check(world.CrossingCount==1,"A blue to white");Audit("after-A");stage=5;}break;
                    case 5:move=1;if(x>=11.04f){phaseTime=Time.time;stage=6;}break;
                    case 6:
                        move=1;jump=actor.Grounded;
                        if(Time.time-phaseTime>1.4f){Check(x<=11.061f && world.CrossingCount==1,"white route cannot jump the 120px forward barrier");stage=7;}break;
                    case 7:
                        move=x>2.0f?-1:0;jump=JumpForStep(-1);
                        if(x<=2.0f && actor.Grounded){move=0;jump=true;phaseTime=Time.time;stage=8;}break;
                    case 8:if(Time.time-phaseTime>.18f){dive=true;stage=9;}break;
                    case 9:if(!world.Mirrored){Check(world.CrossingCount==2,"B white to blue");Check(Mathf.Abs(actor.Foot.y+2.8f)<.0001f,"B lands on initial unreachable upper-left platform");Audit("after-B");stage=10;}break;
                    case 10:
                        move=1;jump=JumpForStep(1);
                        if(world.Complete)
                        {
                            Check(actor.Blue && world.CrossingCount==2,"two crossings reach second-level blue exit");
                            if(continueToThird)
                            {
                                Check(manager.IsTransitioning,"shared transition to third started");
                                File.WriteAllText("artifacts/first-two-levels-regression.txt","PASS\n"+report);
                                Application.logMessageReceived-=OnLog;actor.ReleaseValidationInput();enabled=false;return;
                            }
                            Check(string.IsNullOrEmpty(world.nextSceneName)?!manager.IsTransitioning:manager.IsTransitioning,"configured next-scene behaviour");
                            Finish(true,"first level -> black transition -> rebuilt second level PASS");return;
                        }break;
                }
                actor.SetInputForValidation(move,jump,dive);
            }
            catch(Exception e){Finish(false,e.Message+" stage="+stage+" foot="+(actor?actor.Foot.ToString():"none"));}
        }
        private void Finish(bool pass,string details)
        {
            Application.logMessageReceived-=OnLog;
            File.WriteAllText("artifacts/independent-levels-validation.txt",(pass?"PASS":"FAIL")+"\n"+details+"\n"+report);
            SessionState.SetBool("IndependentLevelsValidation",false);if(actor)actor.ReleaseValidationInput();enabled=false;
            if(pass)Debug.Log("INDEPENDENT_LEVELS_PASS "+details);else Debug.LogError("INDEPENDENT_LEVELS_FAIL "+details);
            EditorApplication.delayCall+=()=>
            {
                EditorApplication.isPlaying=false;
                if(Application.isBatchMode)EditorApplication.delayCall+=()=>EditorApplication.Exit(pass?0:1);
            };
        }
    }
}
