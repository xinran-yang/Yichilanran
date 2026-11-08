using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace YiChiLanRan.Editor
{
    public static class SceneSmoke
    {
        static int stage;
        static double started;
        static bool oldEnabled;
        static EnterPlayModeOptions oldOptions;
        static readonly StringBuilder report=new StringBuilder();
        public static void Full() { RegressionChecks.All(); Start(); }
        public static void Start()
        {
            if(!Application.isBatchMode) throw new Exception("Scene smoke requires isolated batch copy");
            report.Clear(); report.AppendLine("Unity "+Application.unityVersion+" actual Play Mode lifecycle smoke");
            oldEnabled=EditorSettings.enterPlayModeOptionsEnabled; oldOptions=EditorSettings.enterPlayModeOptions;
            EditorSettings.enterPlayModeOptionsEnabled=true; EditorSettings.enterPlayModeOptions=EnterPlayModeOptions.DisableDomainReload;
            EditorSceneManager.OpenScene(GreyboxBuilder.Root+"Scenes/Level_2_1.unity");
            started=EditorApplication.timeSinceStartup; stage=-1;
            Application.logMessageReceived+=Log; EditorApplication.update+=Tick;
            // Let editor startup/search-index delay callbacks settle before entering Play Mode.
        }
        static void Log(string message,string stack,LogType type)
        {
            if(stack.Contains("UnityEditor.Search.SearchDatabase") && !stack.Contains("YiChiLanRan"))
            { report.AppendLine("EDITOR INFRASTRUCTURE: Unity Search startup index exception (not gameplay stack): "+message); return; }
            if(type==LogType.Error || type==LogType.Exception || type==LogType.Assert)
                Finish(false,"Runtime error: "+message+"\n"+stack);
        }
        static void Tick()
        {
            try
            {
                if(EditorApplication.timeSinceStartup-started>35) { Finish(false,"Timeout at stage "+stage); return; }
                if(stage==-1) { if(EditorApplication.timeSinceStartup-started>1) { stage=0; EditorApplication.EnterPlaymode(); } return; }
                if(!EditorApplication.isPlaying) return;
                var w=UnityEngine.Object.FindFirstObjectByType<LevelRuntime>();
                if(!w || w.Player==null || w.Clock<.5f) return;
                if(stage==0)
                {
                    if(w.definition.levelId!="2.1" || !w.Player.Grounded || w.Harness || !Mathf.Approximately(Time.fixedDeltaTime,Rules.Step)) throw new Exception("2.1 production Start/FixedUpdate failed");
                    report.AppendLine("PASS actual Level_2_1 scene starts, player falls/lands via production FixedUpdate at 120 Hz");
                    w.Player.ExternalInput=true; w.Player.Command(0,false,true); stage=1;
                }
                else if(stage==1 && w.Clock>.9f)
                {
                    if(w.Transfers!=0 || w.Player.Diving) throw new Exception("Ground Dive unexpectedly transfers");
                    report.AppendLine("PASS ground Dive is rejected in production player loop");
                    w.Player.Entity.SetPhase(Phase.White); w.Player.Entity.Place(w.Goal.position);
                    if(GoalTrigger.Reached(w)) throw new Exception("White player can complete blue flag");
                    w.Player.Entity.SetPhase(Phase.Blue); PhaseCollisionSystem.Refresh(w); w.Player.Entity.Place(w.Goal.position); stage=2;
                }
                else if(stage==2 && w.definition.levelId=="2.2")
                {
                    if(w.Transfers!=0 || w.Mirrored || w.Player.Entity.phase!=Phase.Blue || w.Harness) throw new Exception("2.2 starts with stale state");
                    report.AppendLine("PASS 2.1 goal coroutine performs short fade and SceneManager loads independent Level_2_2");
                    w.Player.Entity.Place(w.Goal.position); stage=3;
                }
                else if(stage==3 && w.Complete)
                {
                    if(!string.IsNullOrEmpty(w.definition.nextScene)) throw new Exception("2.2 unexpectedly has next scene");
                    report.AppendLine("PASS 2.2 completes and retains scene for completion UI");
                    LevelReset.Restart(w); stage=4;
                }
                else if(stage==4 && !w.Complete && w.Player.Grounded && w.Clock>.7f)
                {
                    report.AppendLine("PASS production reset path (same API used by R) restores playable scene after completion");
                    report.AppendLine("LIMIT Goal contacts are fixtures in this smoke; full input-driven puzzle routes are recorded separately.");
                    report.AppendLine("LIMIT Headless smoke does not verify visual fade/UI appearance or physical keyboard events.");
                    Finish(true,"");
                }
            }
            catch(Exception e) { Finish(false,e.ToString()); }
        }
        static void Finish(bool success,string message)
        {
            EditorApplication.update-=Tick; Application.logMessageReceived-=Log;
            EditorSettings.enterPlayModeOptionsEnabled=oldEnabled; EditorSettings.enterPlayModeOptions=oldOptions;
            if(!success) report.AppendLine("FAIL "+message);
            Directory.CreateDirectory("DevReports"); File.WriteAllText("DevReports/PlayModeSmoke.txt",report.ToString());
            EditorApplication.Exit(success ? 0:1);
        }
    }
}

