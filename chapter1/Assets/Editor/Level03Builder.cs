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
    public static class Level03Builder
    {
        public const string ScenePath="Assets/Scenes/Chapter01_Level03.unity";
        public const string CollectiblePath="Assets/Prefabs/Collectible.prefab";
        [MenuItem("YiChiLanRan/Build Level03 and Link")]
        public static void Generate()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play first");
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())throw new OperationCanceledException();
            var second=EditorSceneManager.OpenScene(IndependentLevelsBuilder.SecondScene);
            var template=UnityEngine.Object.FindFirstObjectByType<ChapterWorld>();
            // Do not regenerate either previous level. Verify the second still matches its saved data.
            var p=Configuration.GameParameters.Load();var secondData=ChapterData.LoadResource("Configuration/Level02");
            var expected=BaseTerrainGeometry.Build(secondData);
            if(template.terrain.Length!=expected.Count)throw new InvalidOperationException("Level02 geometry count changed");
            foreach(var shape in expected)
            {
                var r=p.SourceRectToWorld(shape.pixels);
                if(!template.terrain.Any(t=>t.blue==shape.blue && Vector2.Distance(t.baseCenter,r.center)<.00001f && Vector2.Distance(t.size,r.size)<.00001f))
                    throw new InvalidOperationException("Level02 geometry changed");
            }
            template.nextSceneName="Chapter01_Level03";EditorUtility.SetDirty(template);EditorSceneManager.SaveScene(second);
            var blue=template.terrain.First(t=>t.blue).GetComponent<SpriteRenderer>().sprite;
            var white=template.terrain.First(t=>!t.blue).GetComponent<SpriteRenderer>().sprite;
            var material=template.terrain[0].GetComponent<SpriteRenderer>().sharedMaterial;
            var solid=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Generated/Solid.png");
            var gatePrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/CrossingPoint.prefab");
            var playerPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(IndependentLevelsBuilder.PlayerPrefab);
            var collectiblePrefab=AssetDatabase.LoadAssetAtPath<GameObject>(CollectiblePath);
            if(!collectiblePrefab)collectiblePrefab=MakeCollectible(solid,material);
            var data=ChapterData.LoadResource("Configuration/Level03");
            var third=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(third);
            var root=new GameObject("Level03 · 回纹迷宫");var world=root.AddComponent<ChapterWorld>();
            world.levelResource="Configuration/Level03";world.nextSceneName="";world.showDebugOverlay=false;
            foreach(var shape in BaseTerrainGeometry.Build(data))
            {
                var r=p.SourceRectToWorld(shape.pixels);
                var obj=SpriteNode((shape.blue?"Blue":"White")+" "+shape.pixels,root.transform,shape.blue?blue:white,material,r.size,Color.white,0);
                obj.transform.position=r.center;obj.AddComponent<BoxCollider2D>().size=r.size;
                var piece=obj.AddComponent<TerrainPiece>();piece.blue=shape.blue;piece.size=r.size;piece.baseCenter=r.center;
            }
            for(int i=0;i<data.gates.Length;i++)
            {
                var definition=data.gates[i];var obj=(GameObject)PrefabUtility.InstantiatePrefab(gatePrefab,third);
                obj.name="Portal_"+(char)('A'+i);obj.transform.SetParent(root.transform,false);
                var gate=obj.GetComponent<CrossingPoint>();gate.left=p.ToWorld(definition.leftPx);gate.right=p.ToWorld(definition.rightPx);gate.baseSurface=-p.ToWorld(definition.surfacePx);
                gate.Apply(false,-p.ToWorld(data.mirrorAxisPx));obj.transform.Find("Boundary marker").GetComponent<SpriteRenderer>().size=new Vector2(gate.right-gate.left,.025f);
            }
            foreach(var definition in data.collectibles)
            {
                var obj=(GameObject)PrefabUtility.InstantiatePrefab(collectiblePrefab,third);obj.transform.SetParent(root.transform,false);
                var item=obj.GetComponent<Collectible>();item.persistentId=definition.id;item.baseCenter=p.SourcePointToWorld(definition.centerPx);obj.transform.position=item.baseCenter;
            }
            var person=(GameObject)PrefabUtility.InstantiatePrefab(playerPrefab,third);person.transform.SetParent(root.transform,false);person.transform.position=p.SourcePointToWorld(data.spawnCenterPx);
            world.player=person.GetComponent<ClothPlayer>();world.player.world=world;
            var camera=UnityEngine.Object.Instantiate(template.gameCamera.gameObject);camera.name="Main Camera";world.gameCamera=camera.GetComponent<Camera>();
            camera.transform.position=new Vector3(p.ReferenceViewportSize.x/2,-p.ToWorld(data.cameraCenterYPx),-10);
            world.terrain=root.GetComponentsInChildren<TerrainPiece>();world.gates=root.GetComponentsInChildren<CrossingPoint>();
            var goal=new GameObject("Exit · continue right");goal.transform.SetParent(root.transform,false);goal.transform.position=p.SourcePointToWorld(data.exitFootPx)+Vector2.up*.275f;
            foreach(float x in new[]{-.24f,.24f})SpriteNode("Pavilion post",goal.transform,solid,material,new Vector2(.035f,.55f),ClothArt.Pattern,4).transform.localPosition=new Vector3(x,0);
            SpriteNode("Pavilion lintel",goal.transform,solid,material,new Vector2(.62f,.035f),ClothArt.Pattern,4).transform.localPosition=new Vector3(0,.25f);
            Line("Pavilion roof",goal.transform,solid,material,new Vector2(-.4f,.3f),new Vector2(0,.48f),.035f,ClothArt.Pattern);
            Line("Pavilion roof",goal.transform,solid,material,new Vector2(0,.48f),new Vector2(.4f,.3f),.035f,ClothArt.Pattern);
            var manager=new GameObject("LevelTransitionManager");manager.AddComponent<LevelTransitionManager>();
            EditorSceneManager.SaveScene(third,ScenePath);EditorSceneManager.CloseScene(third,true);SceneManager.SetActiveScene(second);
            var scenes=EditorBuildSettings.scenes.Where(s=>s.path!=ScenePath).ToList();scenes.Add(new EditorBuildSettingsScene(ScenePath,true));EditorBuildSettings.scenes=scenes.ToArray();
            AssetDatabase.SaveAssets();Debug.Log("LEVEL03_GENERATED: three portals, true Greek-key geometry, shared assets and envelope; previous geometry preserved");
        }
        private static GameObject MakeCollectible(Sprite solid,Material material)
        {
            var root=new GameObject("Collectible · folded cloth letter");root.AddComponent<BoxCollider2D>().size=new Vector2(.36f,.24f);root.GetComponent<BoxCollider2D>().isTrigger=true;
            var item=root.AddComponent<Collectible>();item.persistentId="letter";
            var visual=new GameObject("Visual").transform;visual.SetParent(root.transform,false);item.visual=visual;
            SpriteNode("Envelope outline",visual,solid,material,new Vector2(.36f,.24f),ClothArt.Blue,8);
            SpriteNode("Envelope cloth",visual,solid,material,new Vector2(.325f,.205f),ClothArt.Cream,9);
            Line("Fold",visual,solid,material,new Vector2(-.15f,.085f),new Vector2(0,-.02f),.014f,ClothArt.Blue);
            Line("Fold",visual,solid,material,new Vector2(0,-.02f),new Vector2(.15f,.085f),.014f,ClothArt.Blue);
            var seal=SpriteNode("Indigo seal",visual,solid,material,new Vector2(.05f,.05f),ClothArt.Pattern,11);seal.transform.localPosition=new Vector3(0,-.015f);seal.transform.localRotation=Quaternion.Euler(0,0,45);
            var prefab=PrefabUtility.SaveAsPrefabAsset(root,CollectiblePath);UnityEngine.Object.DestroyImmediate(root);return prefab;
        }
        private static GameObject SpriteNode(string name,Transform parent,Sprite sprite,Material material,Vector2 size,Color color,int order)
        {
            var obj=new GameObject(name);obj.transform.SetParent(parent,false);var sr=obj.AddComponent<SpriteRenderer>();sr.sprite=sprite;sr.sharedMaterial=material;sr.drawMode=SpriteDrawMode.Tiled;sr.size=size;sr.color=color;sr.sortingOrder=order;return obj;
        }
        private static void Line(string name,Transform parent,Sprite sprite,Material material,Vector2 from,Vector2 to,float width,Color color)
        {
            var d=to-from;var obj=SpriteNode(name,parent,sprite,material,new Vector2(d.magnitude,width),color,10);obj.transform.localPosition=(from+to)/2;obj.transform.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(d.y,d.x)*Mathf.Rad2Deg);
        }
        public static void BatchGenerateAndValidate(){Generate();Level03Validation.Run();}
        public static void BatchRetestThird(){Generate();Level03Validation.RunThirdOnly();}
        public static void BatchBuildPlayer()
        {
            Directory.CreateDirectory("artifacts");
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{IndependentLevelsBuilder.FirstScene,IndependentLevelsBuilder.SecondScene,ScenePath},target=BuildTarget.StandaloneWindows64,locationPathName="artifacts/Level03Build/YiChiLanRan.exe",options=BuildOptions.None});
            File.WriteAllText("artifacts/level03-build.txt",report.summary.result+" errors="+report.summary.totalErrors);
            if(report.summary.result!=BuildResult.Succeeded)throw new InvalidOperationException("Three-level build failed");
        }
    }
}
