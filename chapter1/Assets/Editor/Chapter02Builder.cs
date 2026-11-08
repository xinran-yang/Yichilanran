using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace YiChiLanRan.EditorTools
{
    [InitializeOnLoad]
    public static class Chapter02Builder
    {
        static Chapter02Builder()
        {
            EditorApplication.update += PollRequest;
        }
        private static void PollRequest()
        {
            if(Application.isBatchMode) return;
            const string request="Temp/Chapter02.request";
            if(!File.Exists(request) || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if(EditorApplication.isPlayingOrWillChangePlaymode) { EditorApplication.isPlaying=false; return; }
            File.Delete(request);
            try { Extend(); Chapter02Smoke.Run(); }
            catch(Exception e) { Debug.LogException(e); }
        }
        public static void BatchBuildAndValidate()
        {
            if(File.Exists("Temp/Chapter02.request")) File.Delete("Temp/Chapter02.request");
            Extend(); Chapter02Smoke.Run();
        }
        public static bool BuildWindowsPlayer()
        {
            if(!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone,BuildTarget.StandaloneWindows64))
            {
                File.WriteAllText("artifacts/level02-build.txt","SKIP: Windows player build module unavailable"); return true;
            }
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes=new[]{Chapter01Builder.ScenePath}, locationPathName="artifacts/Build/YiChiLanRan.exe",
                target=BuildTarget.StandaloneWindows64, options=BuildOptions.None });
            File.WriteAllText("artifacts/level02-build.txt",report.summary.result+" errors="+report.summary.totalErrors);
            return report.summary.result==BuildResult.Succeeded;
        }
        public static void BatchReleaseBuild()
        {
            if(!BuildWindowsPlayer()) throw new InvalidOperationException("Windows release build failed");
        }
        [MenuItem("YiChiLanRan/Extend Chapter01 with Level02")]
        public static void Extend()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play first.");
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) throw new OperationCanceledException();
            var scene=EditorSceneManager.OpenScene(Chapter01Builder.ScenePath);
            var world=UnityEngine.Object.FindFirstObjectByType<ChapterWorld>();
            var data=ChapterData.Load(); var p=Configuration.GameParameters.Load();
            var old=world.transform.Find("Level02"); if(old) UnityEngine.Object.DestroyImmediate(old.gameObject);
            old=world.transform.Find("Level01Connection"); if(old) UnityEngine.Object.DestroyImmediate(old.gameObject);
            var region=new GameObject("Level02").transform; region.SetParent(world.transform,false);
            var connection=new GameObject("Level01Connection").transform; connection.SetParent(world.transform,false);
            var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Generated/ClothUnlit.mat");
            var blue=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Generated/IndigoCloth.png");
            var white=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Generated/IvoryCloth.png");
            var solid=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Generated/Solid.png");
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/CrossingPoint.prefab");
            if(!material || !blue || !white || !solid || !prefab) throw new InvalidOperationException("Original Level01 assets missing; extension will not invent replacements.");
            float start=data.regions[1].startPx;
            foreach(var segment in data.segments.Where(s=>s.xPx>=start))
                foreach(bool isBlue in new[]{true,false})
                {
                    float y=isBlue?segment.surfacePx:data.topPx;
                    float height=isBlue?data.bottomPx-segment.surfacePx:segment.surfacePx-data.topPx;
                    Rect rect=p.SourceRectToWorld(new Rect(segment.xPx,y,segment.widthPx,height));
                    var obj=SpriteObject((isBlue?"Blue":"White")+" contour "+segment.xPx,region,isBlue?blue:white,material,rect.size,0);
                    obj.transform.position=rect.center;
                    obj.AddComponent<BoxCollider2D>().size=rect.size;
                    var piece=obj.AddComponent<TerrainPiece>(); piece.blue=isBlue; piece.size=rect.size; piece.baseCenter=rect.center;
                }
            foreach(var definition in data.gates.Where(g=>g.leftPx>data.regions[0].exitPx))
            {
                var obj=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);
                obj.transform.SetParent(definition.leftPx<start?connection:region,false);
                var gate=obj.GetComponent<CrossingPoint>();
                gate.left=p.ToWorld(definition.leftPx); gate.right=p.ToWorld(definition.rightPx); gate.baseSurface=-p.ToWorld(definition.surfacePx);
                gate.Apply(false,-p.ToWorld(data.mirrorAxisPx));
                obj.transform.Find("Boundary marker").GetComponent<SpriteRenderer>().size=new Vector2(gate.right-gate.left,.025f);
            }
            foreach(var cp in data.checkpoints.Skip(1)) MakeMarker(cp.id,cp.xPx,cp.surfacePx,region,solid,material,p);
            var exit=data.segments.First(s=>data.regions[1].exitPx>=s.xPx && data.regions[1].exitPx<s.xPx+s.widthPx);
            MakeMarker("level02-exit",data.regions[1].exitPx,exit.surfacePx,region,solid,material,p);
            world.terrain=world.GetComponentsInChildren<TerrainPiece>();
            world.gates=world.GetComponentsInChildren<CrossingPoint>().OrderBy(g=>g.left).ToArray();
            world.showDebugOverlay=false;
            EditorUtility.SetDirty(world);
            EditorSceneManager.SaveScene(scene,Chapter01Builder.ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("CHAPTER02_BUILD_OK: same Chapter01 scene; original player/art/prefab preserved; "+world.terrain.Length+" terrain pieces, "+world.gates.Length+" gates.");
        }
        private static GameObject SpriteObject(string name,Transform parent,Sprite sprite,Material material,Vector2 size,int order)
        {
            var obj=new GameObject(name); obj.transform.SetParent(parent,false);
            var sr=obj.AddComponent<SpriteRenderer>(); sr.sprite=sprite; sr.sharedMaterial=material;
            sr.drawMode=SpriteDrawMode.Tiled; sr.size=size; sr.sortingOrder=order;
            return obj;
        }
        private static void MakeMarker(string id,float x,float y,Transform parent,Sprite solid,Material material,Configuration.GameParameters p)
        {
            var obj=SpriteObject(id,parent,solid,material,new Vector2(.035f,.55f),4);
            var flag=SpriteObject("Flag",obj.transform,solid,material,new Vector2(.23f,.14f),4);
            flag.transform.localPosition=new Vector3(.12f,.18f);
            var marker=obj.AddComponent<ProgressMarker>(); marker.checkpointId=id; marker.baseFoot=p.SourcePointToWorld(new Vector2(x,y));
            marker.Apply(false,0,false);
        }
    }
}
