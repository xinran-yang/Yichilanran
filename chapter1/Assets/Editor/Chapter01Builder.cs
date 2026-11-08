using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using YiChiLanRan.Configuration;

namespace YiChiLanRan.EditorTools
{
    public static class Chapter01Builder
    {
        public const string ScenePath = "Assets/Scenes/Chapter01.unity";
        private const string ArtPath = "Assets/Art/Generated";
        private const string PrefabPath = "Assets/Prefabs/CrossingPoint.prefab";

        [InitializeOnLoadMethod]
        private static void ScheduleFirstBuild()
        {
            EditorApplication.delayCall += () =>
            {
                if (!File.Exists(ScenePath) && !EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling)
                {
                    try { Build(); }
                    catch (Exception e) { Debug.LogException(e); }
                }
            };
        }
        [MenuItem("YiChiLanRan/Build Chapter01")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play mode before building.");
            Directory.CreateDirectory(ArtPath); Directory.CreateDirectory("Assets/Prefabs"); Directory.CreateDirectory("Assets/Scenes");
            AssetDatabase.Refresh();
            var p = GameParameters.Load(); var d = ChapterData.Load(false);
            var material = AssetDatabase.LoadAssetAtPath<Material>(ArtPath + "/ClothUnlit.mat");
            if (!material)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
                AssetDatabase.CreateAsset(material, ArtPath + "/ClothUnlit.mat");
            }
            Sprite blue = MakeSprite("IndigoCloth", 64, 64, (x,y) => Fabric(x,y,true), p.PixelsPerUnit);
            Sprite white = MakeSprite("IvoryCloth", 64, 64, (x,y) => Fabric(x,y,false), p.PixelsPerUnit);
            Sprite solid = MakeSprite("Solid", 8, 8, (x,y) => Color.white, p.PixelsPerUnit);
            Sprite spirit = MakeSprite("ClothSpirit", 28, 38, (x,y) =>
            {
                bool inside = y < 24 ? x >= 2 && x < 26 && y >= ((x/5)%2) * 2 : (new Vector2(x-13.5f,y-24)).sqrMagnitude <= 13.5f*13.5f;
                return inside ? Color.white : Color.clear;
            }, p.PixelsPerUnit);
            Sprite eye = MakeSprite("Eye", 8, 8, (x,y) => new Vector2(x-3.5f,y-3.5f).sqrMagnitude <= 12 ? Color.white : Color.clear, p.PixelsPerUnit);
            Sprite lotus = MakeSprite("Lotus", 64, 24, (x,y) =>
            {
                float xx=(x-31.5f)/28, yy=(y-2)/20f;
                bool petal = yy >= 0 && yy <= 1 && (Mathf.Abs(xx) < .24f * Mathf.Sin(yy*Mathf.PI) || Mathf.Abs(xx-(1-yy)*.65f)<.22f*Mathf.Sin(yy*Mathf.PI) || Mathf.Abs(xx+(1-yy)*.65f)<.22f*Mathf.Sin(yy*Mathf.PI));
                return petal ? ClothArt.Pattern : Color.clear;
            }, p.PixelsPerUnit);
            // Prefab's trigger is a marked analytic plane; no broad overlap trigger can grant a switch.
            var gateTemplate = new GameObject("CrossingPoint"); gateTemplate.AddComponent<CrossingPoint>();
            var line = SpriteObject("Boundary marker", gateTemplate.transform, solid, material, ClothArt.Pattern, new Vector2(1.2f,.025f), 5);
            line.transform.localPosition = Vector3.zero;
            var flower = SpriteObject("Lotus", gateTemplate.transform, lotus, material, Color.white, new Vector2(.64f,.24f), 6);
            flower.transform.localPosition = new Vector3(0,.12f);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(gateTemplate, PrefabPath);
            UnityEngine.Object.DestroyImmediate(gateTemplate);

            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            var root = new GameObject("Chapter01 · 留白"); var world = root.AddComponent<ChapterWorld>();
            var pieces = new List<TerrainPiece>();
            foreach (var segment in d.segments)
            {
                foreach (bool isBlue in new[] {true,false})
                {
                    float y = isBlue ? segment.surfacePx : d.topPx;
                    float height = isBlue ? d.bottomPx-segment.surfacePx : segment.surfacePx-d.topPx;
                    Rect rect = p.SourceRectToWorld(new Rect(segment.xPx,y,segment.widthPx,height));
                    var obj = SpriteObject((isBlue?"Blue":"White")+" contour "+segment.xPx, root.transform, isBlue?blue:white, material, Color.white, rect.size, 0);
                    obj.transform.position = rect.center;
                    var shape = obj.AddComponent<BoxCollider2D>(); shape.size = rect.size;
                    var piece = obj.AddComponent<TerrainPiece>(); piece.blue=isBlue; piece.baseCenter=rect.center; piece.size=rect.size;
                    pieces.Add(piece);
                    // Tiny inset seam marks the exact visual contour without changing the collider.
                }
            }
            var gateList = new List<CrossingPoint>();
            foreach (var definition in d.gates)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                instance.transform.SetParent(root.transform);
                var gate = instance.GetComponent<CrossingPoint>();
                gate.left=p.ToWorld(definition.leftPx); gate.right=p.ToWorld(definition.rightPx); gate.baseSurface=-p.ToWorld(definition.surfacePx);
                gate.Apply(false,-p.ToWorld(d.mirrorAxisPx));
                instance.transform.Find("Boundary marker").GetComponent<SpriteRenderer>().size = new Vector2(gate.right-gate.left,.025f);
                gateList.Add(gate);
            }
            var person = SpriteObject("布灵 · Player (foot anchor)", root.transform, spirit, material, ClothArt.Blue, p.PlayerVisualSize, 10);
            person.GetComponent<SpriteRenderer>().drawMode = SpriteDrawMode.Simple;
            person.AddComponent<BoxCollider2D>().size=p.PlayerColliderSize;
            var actor = person.AddComponent<ClothPlayer>(); actor.world=world;
            person.transform.position=p.SourcePointToWorld(d.spawnCenterPx);
            foreach (float x in new[] {-.045f,.045f})
            {
                var eyeObj=SpriteObject("Eye",person.transform,eye,material,ClothArt.Pattern,new Vector2(.05f,.05f),11);
                eyeObj.transform.localPosition=new Vector3(x,.055f);
            }
            var cameraObj=new GameObject("Main Camera"); cameraObj.tag="MainCamera";
            var camera=cameraObj.AddComponent<Camera>(); camera.orthographic=true; camera.orthographicSize=p.ReferenceOrthographicSize;
            camera.backgroundColor=ClothArt.Cream; camera.clearFlags=CameraClearFlags.SolidColor;
            camera.transform.position=new Vector3(p.ReferenceViewportSize.x/2,-p.ReferenceViewportSize.y/2,-10);
            cameraObj.AddComponent<AudioListener>();
            world.player=actor; world.gameCamera=camera; world.terrain=pieces.ToArray(); world.gates=gateList.ToArray();
            Marker("Checkpoint",p.SourcePointToWorld(d.checkpointFootPx),root.transform,solid,material,ClothArt.Pattern);
            Marker("Exit · continue right",p.SourcePointToWorld(d.exitFootPx),root.transform,solid,material,ClothArt.Blue);
            EditorSceneManager.SaveScene(scene,ScenePath);
            EditorSceneManager.CloseScene(scene,true);
            if(previous.IsValid()) SceneManager.SetActiveScene(previous);
            EditorBuildSettings.scenes=new[] {new EditorBuildSettingsScene(ScenePath,true)};
            AssetDatabase.SaveAssets();
            Debug.Log("CHAPTER01_BUILD_OK: scene, gate prefab and independent cloth sprites saved.");
        }
        private static void Marker(string name,Vector2 foot,Transform parent,Sprite sprite,Material mat,Color colour)
        {
            var post=SpriteObject(name,parent,sprite,mat,colour,new Vector2(.035f,.55f),4);
            post.transform.position=foot+Vector2.up*.275f;
            var flag=SpriteObject("Flag",post.transform,sprite,mat,colour,new Vector2(.23f,.14f),4);
            flag.transform.localPosition=new Vector3(.12f,.18f);
        }
        private static GameObject SpriteObject(string name,Transform parent,Sprite sprite,Material mat,Color colour,Vector2 size,int order)
        {
            var obj=new GameObject(name); obj.transform.SetParent(parent,false);
            var sr=obj.AddComponent<SpriteRenderer>(); sr.sprite=sprite; sr.sharedMaterial=mat; sr.color=colour;
            sr.drawMode=SpriteDrawMode.Tiled; sr.size=size; sr.sortingOrder=order; return obj;
        }
        private static Color Fabric(int x,int y,bool blue)
        {
            float noise = ((x*17+y*29+x*y*3)%19)/19f;
            Color c=Color.Lerp(blue?ClothArt.Blue:ClothArt.Cream,blue?new Color(.14f,.31f,.45f):Color.white,noise*.08f);
            float xx=x-32, yy=y-32, r=Mathf.Sqrt(xx*xx+yy*yy), angle=Mathf.Atan2(yy,xx);
            float petals=12+7*Mathf.Cos(6*angle);
            bool flower = r<petals && r>petals-2 || r<3 || Mathf.Abs(xx+yy*.5f)<.8f && y<17;
            if(flower) c=Color.Lerp(c,blue?ClothArt.Pattern:ClothArt.Blue,blue?.65f:.12f);
            return c;
        }
        private static Sprite MakeSprite(string name,int width,int height,Func<int,int,Color> pixel,int ppu)
        {
            string path=ArtPath+"/"+name+".png";
            var tex=new Texture2D(width,height,TextureFormat.RGBA32,false);
            var colors=new Color[width*height];
            for(int y=0;y<height;y++) for(int x=0;x<width;x++) colors[y*width+x]=pixel(x,y);
            tex.SetPixels(colors); tex.Apply(); File.WriteAllBytes(path,tex.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType=TextureImporterType.Sprite; importer.spritePixelsPerUnit=ppu;
            importer.spriteImportMode=SpriteImportMode.Single;
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            settings.spriteMeshType=SpriteMeshType.FullRect; importer.SetTextureSettings(settings);
            importer.filterMode=FilterMode.Point; importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency=true; importer.wrapMode=TextureWrapMode.Repeat; importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        [MenuItem("YiChiLanRan/Open Chapter01")]
        public static void Open()
        {
            if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(ScenePath);
        }
    }
}
