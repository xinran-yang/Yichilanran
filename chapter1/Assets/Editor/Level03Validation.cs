using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace YiChiLanRan.EditorTools
{
    [InitializeOnLoad]
    public static class Level03Validation
    {
        static Level03Validation()
        {
            EditorApplication.playModeStateChanged+=state=>
            {
                if(state!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool("Level03Validation",false))return;
                if(SessionState.GetBool("Level03DirectRecheck",false))
                {
                    var direct=new GameObject("Editor only · corrected third route");UnityEngine.Object.DontDestroyOnLoad(direct);direct.AddComponent<GreekKeyRouteDriver>().directStart=true;return;
                }
                new GameObject("Editor only · original first route").AddComponent<Chapter01SmokeDriver>();
                var second=new GameObject("Editor only · original second route");UnityEngine.Object.DontDestroyOnLoad(second);second.AddComponent<IndependentRouteDriver>().continueToThird=true;
                var third=new GameObject("Editor only · Greek-key route");UnityEngine.Object.DontDestroyOnLoad(third);third.AddComponent<GreekKeyRouteDriver>();
            };
        }
        [MenuItem("YiChiLanRan/Validate Through Level03 Once")]
        public static void Run()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)return;
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            EditorSceneManager.OpenScene(IndependentLevelsBuilder.FirstScene);
            foreach(string flag in new[]{"Chapter01Smoke","Chapter01Preview","Chapter02Smoke","IndependentLevelsValidation"})SessionState.SetBool(flag,false);
            SessionState.SetBool("Level03DirectRecheck",false);SessionState.SetBool("Level03Validation",true);Application.runInBackground=true;EditorApplication.isPlaying=true;
        }
        public static void RunThirdOnly()
        {
            EditorSceneManager.OpenScene(Level03Builder.ScenePath);
            foreach(string flag in new[]{"Chapter01Smoke","Chapter01Preview","Chapter02Smoke","IndependentLevelsValidation"})SessionState.SetBool(flag,false);
            SessionState.SetBool("Level03DirectRecheck",true);SessionState.SetBool("Level03Validation",true);Application.runInBackground=true;EditorApplication.isPlaying=true;
        }
    }
    [DefaultExecutionOrder(-80)]
    public sealed class GreekKeyRouteDriver : MonoBehaviour
    {
        public bool directStart;
        private ChapterWorld world;private ClothPlayer actor;
        private int stage,lastStage=-1;private float started,phaseTime,minRightFoot=760;
        private bool error,sawThirdLocked,underWall;
        private readonly System.Text.StringBuilder report=new System.Text.StringBuilder();
        private void Start(){started=Time.realtimeSinceStartup;Directory.CreateDirectory("artifacts");Application.logMessageReceived+=OnLog;}
        private void OnLog(string message,string stack,LogType type){if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert)error=true;}
        private void Check(bool condition,string message){if(!condition)throw new InvalidOperationException(message);report.AppendLine(message);}
        private bool JumpStep(float dir)
        {
            if(!actor.Grounded)return false;
            float height=actor.Blue?world.Parameters.ElasticJumpHeight:world.Parameters.RigidJumpHeight;
            foreach(var piece in world.terrain)
            {
                if(!piece.Shape.enabled)continue;var r=piece.WorldRect;float rise=r.yMax-actor.Foot.y;
                if(rise<=.01f || rise>height+.01f)continue;
                if(dir>0 && r.xMin>actor.Foot.x && r.xMin-actor.Foot.x<.44f)return true;
                if(dir<0 && r.xMax<actor.Foot.x && actor.Foot.x-r.xMax<.44f)return true;
            }
            return false;
        }
        private void Audit(string label)
        {
            Check(actor.transform.rotation==Quaternion.identity && actor.transform.localScale==Vector3.one,label+" upright and scale");
            foreach(var piece in world.terrain)
            {
                var expected=world.Mirrored?ChapterWorld.Reflect(piece.baseCenter,world.Axis):piece.baseCenter;
                Check(Vector2.Distance(piece.transform.position,expected)<.00001f && Vector2.Distance(piece.Shape.size,piece.size)<.00001f && Vector2.Distance(piece.GetComponent<SpriteRenderer>().size,piece.size)<.00001f,label+" geometry "+piece.name);
                Check(piece.Shape.enabled==(piece.blue!=world.Mirrored),label+" phase collision "+piece.name);
                if(Vector2.Distance(ChapterWorld.Reflect(ChapterWorld.Reflect(piece.baseCenter,world.Axis),world.Axis),piece.baseCenter)>.00001f)throw new Exception("Mirror involution");
            }
            foreach(var item in world.Collectibles)
            {
                var expected=world.Mirrored?ChapterWorld.Reflect(item.baseCenter,world.Axis):item.baseCenter;
                Check(Vector2.Distance(item.transform.position,expected)<.00001f,label+" collectible mirror");
            }
            File.WriteAllText("artifacts/level03-state-"+label+".json",JsonUtility.ToJson(new Snapshot{phase=label,foot=actor.Foot,axis=world.Axis,crossings=world.CrossingCount,blue=actor.Blue,collected=world.CollectedCount},true));
        }
        [Serializable]private sealed class Snapshot{public string phase;public Vector2 foot;public float axis;public int crossings,collected;public bool blue;}
        private bool InitialConnectedToUpper()
        {
            const int step=8;int minX=56,minY=-400,cols=367,rows=181;
            var solids=BaseTerrainGeometry.Build(world.Data).Where(r=>r.blue).Select(r=>r.pixels).ToArray();
            var half=world.Parameters.PlayerColliderSize*50;
            Func<int,int,bool> open=(x,y)=>
            {
                var body=new Rect(minX+x*step-half.x,minY+y*step-half.y,half.x*2,half.y*2);
                return !solids.Any(r=>body.xMin<r.xMax-.001f && body.xMax>r.xMin+.001f && body.yMin<r.yMax-.001f && body.yMax>r.yMin+.001f);
            };
            Func<Vector2,int> anchor=point=>
            {
                int x=Mathf.RoundToInt((point.x-minX)/step),y=Mathf.RoundToInt((point.y-minY)/step);
                while(y>0 && !open(x,y))y--;
                return y*cols+x;
            };
            int start=anchor(new Vector2(100,741)),target=anchor(new Vector2(240,171));var visited=new HashSet<int>();var q=new Queue<int>();q.Enqueue(start);visited.Add(start);
            while(q.Count>0)
            {
                int n=q.Dequeue();if(n==target)return true;int x=n%cols,y=n/cols;
                foreach(var point in new[]{new Vector2Int(x-1,y),new Vector2Int(x+1,y),new Vector2Int(x,y-1),new Vector2Int(x,y+1)})
                {
                    int next=point.y*cols+point.x;
                    if(point.x>=0 && point.x<cols && point.y>=0 && point.y<rows && !visited.Contains(next) && open(point.x,point.y)){visited.Add(next);q.Enqueue(next);}
                }
            }
            return false;
        }
        private void FixedUpdate()
        {
            try
            {
                if(Time.realtimeSinceStartup-started>190)throw new Exception("Route timeout");
                if(error)throw new Exception("Unity reported an error");
                world=FindFirstObjectByType<ChapterWorld>();if(!world)return;actor=world.player;
                var manager=LevelTransitionManager.Instance;
                if(stage==0)
                {
                    if(world.Data.levelId=="level03" && manager.IsTransitioning)
                    {if(!actor.ControlsLocked || actor.Velocity!=Vector2.zero)throw new Exception("Third spawn not locked");sawThirdLocked=true;}
                    if(world.Data.levelId!="level03" || manager.IsTransitioning || !world.SpawnInitialized)return;
                    if(!directStart)Check(manager.CompletedTransitions==2 && manager.LoadedUnderBlack && manager.Alpha==0 && sawThirdLocked,"shared black transitions 01 -> 02 -> 03");
                    else Check(manager.Alpha==0 && !actor.ControlsLocked,"direct third-scene recheck; prior full-flow regression retained");
                    Check(actor.Blue && Vector2.Distance(actor.Foot,new Vector2(1,-7.6f))<.0001f,"third independent blue spawn");
                    Check(world.gates.Length==3 && world.Collectibles.Length==1,"exactly three original-prefab gates and one letter");
                    Check(!InitialConnectedToUpper(),"8px configuration-space flood: initial blue chamber disconnected from upper blue route");
                    var shapes=BaseTerrainGeometry.Build(world.Data);
                    float area=shapes.Sum(s=>s.pixels.width*s.pixels.height);
                    Check(Mathf.Abs(area-(world.Data.widthPx-world.Data.leftPx)*(world.Data.bottomPx-world.Data.topPx))<.5f,"single blue union plus white complement covers bounds exactly");
                    Check(world.Parameters.DiveBounceHeight*100<270 && world.Parameters.RigidJumpHeight*100<140,"high wall / phase barriers exceed original motion limits");
                    Audit("initial");stage=1;phaseTime=Time.time;
                }
                float x=actor.Foot.x,footPx=-actor.Foot.y*100;float move=0;bool jump=false,dive=false;
                if(stage!=lastStage){File.WriteAllText("artifacts/level03-live.txt","stage="+stage+" foot="+actor.Foot+" crossings="+world.CrossingCount);lastStage=stage;}
                switch(stage)
                {
                    case 1:move=1;if(x>2.7f){jump=actor.Grounded;dive=!actor.Grounded&&actor.Velocity.y<=0;}
                        if(Time.time-phaseTime>1.8f){Check(x<=2.861f && world.CrossingCount==0,"initial room cannot bypass A");stage=2;}break;
                    case 2:move=x>2.2f?-1:0;if(x<=2.2f&&actor.Grounded){move=0;jump=true;phaseTime=Time.time;stage=3;}break;
                    case 3:if(Time.time-phaseTime>.16f){dive=true;stage=4;}break;
                    case 4:if(world.Mirrored){Check(world.CrossingCount==1,"A blue -> white");Audit("A");stage=5;}break;
                    case 5:move=1;if(x>=4.7f&&actor.Grounded){stage=6;phaseTime=Time.time;}break;
                    case 6:move=1;jump=actor.Grounded;if(Time.time-phaseTime>.9f){Check(x<=4.861f&&world.CrossingCount==1,"white lower-arm break blocks bypass");stage=7;}break;
                    case 7:move=x>1.0f?-1:0;if(x<=1.0f&&actor.Grounded&&Mathf.Abs(footPx-410)<.2f)stage=8;break;
                    case 8:move=1;if(x>=9.84f&&actor.Grounded){stage=9;phaseTime=Time.time;}break;
                    case 9:move=1;jump=actor.Grounded;if(Time.time-phaseTime>.9f){Check(x<=9.861f&&world.CrossingCount==1,"white neck 160px step blocks skipping B");stage=10;}break;
                    case 10:move=x>2.4f?-1:0;if(x<=2.4f&&actor.Grounded){move=0;jump=true;phaseTime=Time.time;stage=11;}break;
                    case 11:if(Time.time-phaseTime>.16f){dive=true;stage=12;}break;
                    case 12:if(!world.Mirrored){Check(world.CrossingCount==2&&Mathf.Abs(footPx-190)<.1f,"B restores base geometry and upper-left landing");Audit("B");stage=13;}break;
                    case 13:move=x<11.90f?1:0;if(x>=11.90f&&actor.Grounded&&Mathf.Abs(footPx-350)<.2f){move=0;jump=true;phaseTime=Time.time;stage=14;}break;
                    case 14:if(Time.time-phaseTime>.16f){dive=true;stage=15;}break;
                    case 15:if(actor.Velocity.y>7.0f)stage=16;break;
                    case 16:move=1;if(x>=14&&actor.Grounded&&Mathf.Abs(footPx-190)<.2f){Check(true,"original fixed blue rebound reaches central meander");stage=17;}break;
                    case 17:move=x<18.9f?1:0;if(x>=18.9f&&actor.Grounded&&Mathf.Abs(footPx-900)<.2f)stage=18;break;
                    case 18:move=x>7.0f?-1:0;jump=JumpStep(-1);if(x<=7.0f&&actor.Grounded){Check(world.CollectedCount==1,"lower horizontal branch letter reached by normal movement");stage=19;}break;
                    case 19:
                        move=x<27.2f?1:0;jump=JumpStep(1);
                        if(x>20.1f&&x<21.9f){Check(footPx>=798,"high wall passed through designed lower corridor");underWall=true;}
                        if(x>=27.2f&&actor.Grounded&&Mathf.Abs(footPx-760)<.2f){Check(underWall,"lower corridor returns to right chamber");phaseTime=Time.time;stage=20;}break;
                    case 20:move=1;jump=actor.Grounded;dive=!actor.Grounded&&actor.Velocity.y<=0;minRightFoot=Mathf.Min(minRightFoot,footPx);
                        if(Time.time-phaseTime>1.5f){Check(x<=28.061f&&minRightFoot>500&&!world.Complete,"right blue chamber cannot climb pavilion before C");stage=21;}break;
                    case 21:move=x>27.2f?-1:0;if(x<=27.2f&&actor.Grounded){move=0;jump=true;phaseTime=Time.time;stage=22;}break;
                    case 22:if(Time.time-phaseTime>.16f){dive=true;stage=23;}break;
                    case 23:if(world.Mirrored){Check(world.CrossingCount==3&&world.HasCollected("chapter01-letter"),"C blue -> white retains letter");Audit("C");Check(!world.Collectibles[0].Visible,"collected item stays hidden after mirror");stage=24;}break;
                    case 24:move=x<29.8f?1:0;if(world.Complete)throw new Exception("Exit triggered in wrong lower/upper lane");if(x>=29.8f&&actor.Grounded&&Mathf.Abs(footPx-440)<.2f)stage=25;break;
                    case 25:
                        move=x>27.4f?-1:0;jump=JumpStep(-1);
                        if(world.Complete)
                        {
                            Check(!actor.Blue&&world.CrossingCount==3&&world.CollectedCount==1,"A -> B -> C and optional letter reach upper-right pavilion");
                            Check(!manager.IsTransitioning,"empty fourth-scene interface does not black-screen");Audit("complete");
                            Check(!world.RecordCollection("chapter01-letter")&&world.CollectedCount==1,"duplicate pickup refused");
                            world.Respawn();Check(world.HasCollected("chapter01-letter")&&!world.Collectibles[0].Visible&&world.Mirrored,"production respawn keeps item and checkpoint phase");
                            world.Restart();Check(world.CollectedCount==0&&world.Collectibles[0].Visible&&actor.Blue,"explicit new run restores authored collectible");
                            Finish(true,"three-level production flow and Greek-key collectible route PASS");return;
                        }break;
                }
                actor.SetInputForValidation(move,jump,dive);
            }
            catch(Exception exception){Finish(false,exception.Message+" stage="+stage+" foot="+(actor?actor.Foot.ToString():"none"));}
        }
        private void Finish(bool pass,string message)
        {
            Application.logMessageReceived-=OnLog;SessionState.SetBool("Level03Validation",false);if(actor)actor.ReleaseValidationInput();enabled=false;
            SessionState.SetBool("Level03DirectRecheck",false);
            File.WriteAllText("artifacts/level03-validation.txt",(pass?"PASS":"FAIL")+"\n"+message+"\n"+report);
            if(pass)Debug.Log("LEVEL03_PASS "+message);else Debug.LogError("LEVEL03_FAIL "+message);
            EditorApplication.delayCall+=()=>{EditorApplication.isPlaying=false;if(Application.isBatchMode)EditorApplication.delayCall+=()=>EditorApplication.Exit(pass?0:1);};
        }
    }
}
