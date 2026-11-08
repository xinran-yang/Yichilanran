using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace YiChiLanRan.EditorTools
{
    [InitializeOnLoad]
    public static class Chapter01Smoke
    {
        static Chapter01Smoke()
        {
            EditorApplication.update += () =>
            {
                const string stop = "Temp/Chapter01Stop.request";
                if (!File.Exists(stop)) return;
                File.Delete(stop); SessionState.SetBool("Chapter01Smoke", false);
                EditorApplication.isPlaying = false;
            };
            EditorApplication.delayCall += () =>
            {
                const string preview = "Temp/Chapter01Preview.request";
                if (File.Exists(preview) && !EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    File.Delete(preview);
                    EditorSceneManager.OpenScene(Chapter01Builder.ScenePath);
                    SessionState.SetBool("Chapter01Preview", true);
                    EditorApplication.isPlaying = true;
                    return;
                }
                const string request = "Temp/Chapter01Smoke.request";
                if (!File.Exists(request) || EditorApplication.isPlayingOrWillChangePlaymode) return;
                File.Delete(request);
                Run();
            };
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("Chapter01Smoke", false))
                    new GameObject("Editor only · basic playthrough").AddComponent<Chapter01SmokeDriver>();
                if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("Chapter01Preview", false))
                    new GameObject("Editor only · final visual check").AddComponent<Chapter01Preview>();
            };
        }
        [MenuItem("YiChiLanRan/Validate Chapter01 Once %#F8")]
        public static void Run()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(Chapter01Builder.ScenePath);
            SessionState.SetBool("Chapter01Smoke", true);
            EditorApplication.isPlaying = true;
        }
    }

    public sealed class Chapter01Preview : MonoBehaviour
    {
        private System.Collections.IEnumerator Start()
        {
            yield return new WaitForSeconds(.4f);
            yield return new WaitForEndOfFrame();
            Directory.CreateDirectory("artifacts");
            ScreenCapture.CaptureScreenshot("artifacts/chapter01-final-blue.png");
            var w=FindFirstObjectByType<ChapterWorld>();
            Debug.Assert(w.Ready && w.player.Grounded && !w.Mirrored,"Fresh startup invariant");
            yield return null; yield return null;
            SessionState.SetBool("Chapter01Preview",false);
            Debug.Log("CHAPTER01_FINAL_PREVIEW_OK");
            EditorApplication.isPlaying=false;
        }
    }

    [DefaultExecutionOrder(-100)]
    public sealed class Chapter01SmokeDriver : MonoBehaviour
    {
        private ChapterWorld world;
        private ClothPlayer actor;
        private int stage;
        private float started, jumpAt;
        private bool jumpSent;
        private void Start()
        {
            world = FindFirstObjectByType<ChapterWorld>(); actor = world.player; started = Time.time;
            Directory.CreateDirectory("artifacts");
        }
        private void FixedUpdate()
        {
            if (!actor || !world.Ready) return;
            if (Time.time - started > 35) { Finish(false, "Timed out at stage " + stage + " foot=" + actor.Foot); return; }
            float x = actor.Foot.x;
            float horizontal = 0; bool jump = false, dive = false;
            switch (stage)
            {
                case 0: // Grounded down and ordinary walking must never cross.
                    dive = true; horizontal = 1;
                    if (x >= 2.38f) { horizontal = 0; stage = 1; }
                    break;
                case 1:
                    if (world.Mirrored) { Finish(false,"Walking/grounded down crossed"); return; }
                    jump = true; jumpAt = Time.time; stage = 2; break;
                case 2:
                    if (Time.time - jumpAt > .18f) { dive = true; stage = 3; } break;
                case 3:
                    if (world.Mirrored)
                    {
                        if (world.CrossingCount != 1 || actor.transform.rotation != Quaternion.identity) { Finish(false,"Cross invariant"); return; }
                        foreach (var piece in world.terrain)
                        {
                            var expected = ChapterWorld.Reflect(piece.baseCenter,world.Axis);
                            if (Vector2.Distance(piece.transform.position,expected) > .00001f || Vector2.Distance(piece.Shape.size,piece.size) > .00001f)
                            { Finish(false,"Terrain reflection/collider mismatch"); return; }
                        }
                        if(!Application.isBatchMode) ScreenCapture.CaptureScreenshot("artifacts/chapter01-white.png"); stage=4;
                    }
                    break;
                case 4:
                    horizontal=1;
                    if(x>=2.98f && actor.Grounded) { jump=true; stage=5; }
                    break;
                case 5:
                    horizontal=1;
                    if(x>=5.32f && actor.Grounded) { jump=true; stage=6; }
                    break;
                case 6:
                    horizontal=1;
                    if(x>=7.25f && actor.Grounded) stage=7;
                    break;
                case 7:
                    horizontal=1;
                    if(x>=10.20f && actor.Grounded) { jump=true; stage=8; }
                    break;
                case 8:
                    horizontal=1;
                    if(world.Complete)
                    {
                        if(!Application.isBatchMode) ScreenCapture.CaptureScreenshot("artifacts/chapter01-complete.png");
                        Finish(true,"Movement, legal dive, one crossing, exact terrain/foot reflection, upright player, platform route, checkpoint and exit reached; no scene change.");
                        return;
                    }
                    break;
            }
            actor.SetInputForValidation(horizontal,jump,dive);
        }
        private void Finish(bool passed,string details)
        {
            File.WriteAllText("artifacts/chapter01-smoke.txt",(passed?"PASS":"FAIL")+"\n"+details+"\nfoot="+actor.Foot+"\ncrossings="+world.CrossingCount);
            SessionState.SetBool("Chapter01Smoke",false);
            actor.ReleaseValidationInput();
            if(passed) Debug.Log("CHAPTER01_SMOKE_PASS " + details); else Debug.LogError("CHAPTER01_SMOKE_FAIL " + details);
            // Leave result visible for inspection, but release validation control on stopping Play mode.
            enabled=false;
        }
    }
}
