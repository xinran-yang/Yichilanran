using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace YiChiLanRan.EditorTools
{
    public static class IndependentLevelsBuilder
    {
        public const string FirstScene="Assets/Scenes/Chapter01.unity";
        public const string SecondScene="Assets/Scenes/Chapter01_Level02.unity";
        public const string PlayerPrefab="Assets/Prefabs/ClothPlayer.prefab";
        [MenuItem("YiChiLanRan/Build Independent Levels")]
        public static void Generate()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play before generating scenes");
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())throw new OperationCanceledException();
            var first=EditorSceneManager.OpenScene(FirstScene);
            var firstWorld=UnityEngine.Object.FindFirstObjectByType<ChapterWorld>();
            foreach(string generated in new[]{"Level02","Level01Connection"})
            {
                var node=firstWorld.transform.Find(generated);
                if(node)UnityEngine.Object.DestroyImmediate(node.gameObject);
            }
            firstWorld.terrain=firstWorld.GetComponentsInChildren<TerrainPiece>();
            firstWorld.gates=firstWorld.GetComponentsInChildren<CrossingPoint>();
            firstWorld.levelResource="Configuration/Chapter01";firstWorld.nextSceneName="Chapter01_Level02";firstWorld.showDebugOverlay=false;
            var p=Configuration.GameParameters.Load();var baseFirst=ChapterData.Load(false);
            var expectedFirst=BaseTerrainGeometry.Build(baseFirst);
            if(firstWorld.terrain.Length!=expectedFirst.Count || firstWorld.gates.Length!=1)throw new InvalidOperationException("First level geometry was not preserved");
            foreach(var shape in expectedFirst)
            {
                var r=p.SourceRectToWorld(shape.pixels);
                if(!firstWorld.terrain.Any(t=>t.blue==shape.blue && Vector2.Distance(t.baseCenter,r.center)<.00001f && Vector2.Distance(t.size,r.size)<.00001f))
                    throw new InvalidOperationException("First level rectangle changed");
            }
            var originalPlayer=firstWorld.player;
            originalPlayer.world=null;
            GameObject playerPrefab;
            if(PrefabUtility.IsPartOfPrefabInstance(originalPlayer.gameObject))
                playerPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefab);
            else playerPrefab=PrefabUtility.SaveAsPrefabAssetAndConnect(originalPlayer.gameObject,PlayerPrefab,InteractionMode.AutomatedAction);
            originalPlayer.world=firstWorld;
            AddTransitionRoot(first);
            EditorUtility.SetDirty(firstWorld);EditorUtility.SetDirty(originalPlayer);
            EditorSceneManager.SaveScene(first,FirstScene);

            var blue=firstWorld.terrain.First(t=>t.blue).GetComponent<SpriteRenderer>().sprite;
            var white=firstWorld.terrain.First(t=>!t.blue).GetComponent<SpriteRenderer>().sprite;
            var material=firstWorld.terrain[0].GetComponent<SpriteRenderer>().sharedMaterial;
            var solid=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Generated/Solid.png");
            var gatePrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/CrossingPoint.prefab");
            if(!playerPrefab || !gatePrefab || !blue || !white || !material)throw new InvalidOperationException("Shared first-level assets missing");
            var second=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(second);
            var root=new GameObject("Level02 · layered cloth");var world=root.AddComponent<ChapterWorld>();
            world.levelResource="Configuration/Level02";world.nextSceneName="";world.showDebugOverlay=false;
            var data=ChapterData.LoadResource(world.levelResource);
            foreach(var shape in BaseTerrainGeometry.Build(data))
            {
                var r=p.SourceRectToWorld(shape.pixels);
                var obj=new GameObject((shape.blue?"Blue":"White")+" "+shape.pixels);obj.transform.SetParent(root.transform,false);obj.transform.position=r.center;
                var sr=obj.AddComponent<SpriteRenderer>();sr.sprite=shape.blue?blue:white;sr.sharedMaterial=material;sr.drawMode=SpriteDrawMode.Tiled;sr.size=r.size;
                obj.AddComponent<BoxCollider2D>().size=r.size;
                var piece=obj.AddComponent<TerrainPiece>();piece.baseCenter=r.center;piece.size=r.size;piece.blue=shape.blue;
            }
            for(int i=0;i<data.gates.Length;i++)
            {
                var definition=data.gates[i];var obj=(GameObject)PrefabUtility.InstantiatePrefab(gatePrefab,second);obj.name=i==0?"Portal_A":"Portal_B";obj.transform.SetParent(root.transform,false);
                var gate=obj.GetComponent<CrossingPoint>();gate.left=p.ToWorld(definition.leftPx);gate.right=p.ToWorld(definition.rightPx);gate.baseSurface=-p.ToWorld(definition.surfacePx);
                gate.Apply(false,-p.ToWorld(data.mirrorAxisPx));obj.transform.Find("Boundary marker").GetComponent<SpriteRenderer>().size=new Vector2(gate.right-gate.left,.025f);
            }
            var person=(GameObject)PrefabUtility.InstantiatePrefab(playerPrefab,second);person.transform.SetParent(root.transform,false);person.transform.position=p.SourcePointToWorld(data.spawnCenterPx);
            world.player=person.GetComponent<ClothPlayer>();world.player.world=world;
            var camera=UnityEngine.Object.Instantiate(firstWorld.gameCamera.gameObject);camera.name="Main Camera";camera.transform.position=new Vector3(p.ReferenceViewportSize.x/2,-p.ToWorld(data.cameraCenterYPx),-10);
            world.gameCamera=camera.GetComponent<Camera>();world.gameCamera.orthographicSize=p.ReferenceOrthographicSize;
            world.terrain=root.GetComponentsInChildren<TerrainPiece>();world.gates=root.GetComponentsInChildren<CrossingPoint>();
            var post=SpriteObject("Exit · continue right",root.transform,solid,material,new Vector2(.035f,.55f));post.transform.position=p.SourcePointToWorld(data.exitFootPx)+Vector2.up*.275f;
            var flag=SpriteObject("Flag",post.transform,solid,material,new Vector2(.23f,.14f));flag.transform.localPosition=new Vector3(.12f,.18f);
            AddTransitionRoot(second);
            EditorSceneManager.SaveScene(second,SecondScene);EditorSceneManager.CloseScene(second,true);SceneManager.SetActiveScene(first);
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(FirstScene,true),new EditorBuildSettingsScene(SecondScene,true)};
            AssetDatabase.SaveAssets();
            Debug.Log("INDEPENDENT_LEVELS_GENERATED: first level unchanged; shared player prefab; second level derived from one layered blue shape");
        }
        private static GameObject SpriteObject(string name,Transform parent,Sprite sprite,Material material,Vector2 size)
        {
            var obj=new GameObject(name);obj.transform.SetParent(parent,false);var sr=obj.AddComponent<SpriteRenderer>();sr.sprite=sprite;sr.sharedMaterial=material;sr.drawMode=SpriteDrawMode.Tiled;sr.size=size;sr.sortingOrder=4;sr.color=ClothArt.Pattern;return obj;
        }
        private static void AddTransitionRoot(Scene scene)
        {
            if(scene.GetRootGameObjects().Any(o=>o.GetComponent<LevelTransitionManager>()))return;
            var obj=new GameObject("LevelTransitionManager");SceneManager.MoveGameObjectToScene(obj,scene);obj.AddComponent<LevelTransitionManager>();
        }
        public static void BatchGenerateAndValidate(){Generate();IndependentLevelsValidation.Run();}
        public static void BatchBuildPlayer()
        {
            Directory.CreateDirectory("artifacts");
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{FirstScene,SecondScene},target=BuildTarget.StandaloneWindows64,
                locationPathName="artifacts/ReworkBuild/YiChiLanRan.exe",options=BuildOptions.None});
            File.WriteAllText("artifacts/rework-build.txt",report.summary.result+" errors="+report.summary.totalErrors);
            if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Independent-level player build failed");
        }
    }
}
