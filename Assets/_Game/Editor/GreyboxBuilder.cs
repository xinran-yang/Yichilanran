using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace YiChiLanRan.Editor
{
    public static class GreyboxBuilder
    {
        public const string Root="Assets/_Game/";
        static Sprite square;
        static PhysicsMaterial2D material;
        public static Sprite Square
        {
            get
            {
                if(square) return square;
                string path=Root+"Data/GreyboxSquare.png";
                if(!File.Exists(path))
                {
                    var t=new Texture2D(32,32); var pixels=Enumerable.Repeat(Color.white,1024).ToArray();
                    t.SetPixels(pixels); t.Apply(); File.WriteAllBytes(path,t.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(t); AssetDatabase.ImportAsset(path);
                }
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                if(importer.textureType!=TextureImporterType.Sprite || importer.spritePixelsPerUnit!=32)
                { importer.textureType=TextureImporterType.Sprite; importer.spritePixelsPerUnit=32; importer.filterMode=FilterMode.Point; importer.SaveAndReimport(); }
                return square=AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
        }
        static PhysicsMaterial2D Material
        {
            get
            {
                if(material) return material;
                material=AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(Root+"Data/Greybox.physicsMaterial2D");
                if(!material) { material=new PhysicsMaterial2D("Greybox") {friction=0,bounciness=0}; AssetDatabase.CreateAsset(material,Root+"Data/Greybox.physicsMaterial2D"); }
                return material;
            }
        }
        public static PhaseObject Make(Transform root,string id,Rect rect,Phase phase,ObjectKind kind,bool elastic=false)
        {
            var go=new GameObject(id); go.transform.SetParent(root,false); go.transform.position=rect.center;
            var sprite=go.AddComponent<SpriteRenderer>(); sprite.sprite=Square; sprite.drawMode=SpriteDrawMode.Sliced; sprite.size=rect.size;
            sprite.sortingOrder=kind==ObjectKind.Player ? 10 : kind==ObjectKind.Box ? 5 : kind==ObjectKind.BoxBarrier ? 4 : phase==Phase.Blue ? 1 : phase==Phase.Neutral ? 4 : 3;
            var collider=go.AddComponent<BoxCollider2D>(); collider.size=rect.size; collider.sharedMaterial=Material;
            var o=go.AddComponent<PhaseObject>(); o.objectId=id; o.kind=kind; o.phase=phase; o.elastic=elastic; o.SetPhase(phase);
            if(kind==ObjectKind.Player || kind==ObjectKind.Box)
            {
                var body=go.AddComponent<Rigidbody2D>(); body.gravityScale=0; body.constraints=RigidbodyConstraints2D.FreezeRotation;
                body.collisionDetectionMode=CollisionDetectionMode2D.Continuous; body.interpolation=RigidbodyInterpolation2D.Interpolate; body.mass=kind==ObjectKind.Box ? 5 : 1;
                var inset=new GameObject("Inset"); inset.transform.SetParent(go.transform,false);
                var sr=inset.AddComponent<SpriteRenderer>(); sr.sprite=Square; sr.drawMode=SpriteDrawMode.Sliced; sr.size=rect.size-new Vector2(.10f,.10f); sr.sortingOrder=sprite.sortingOrder+1;
                if(kind==ObjectKind.Player) go.AddComponent<PlayerController2D>(); else go.AddComponent<TransferBox2D>();
                o.SetPhase(phase);
                if(kind==ObjectKind.Box) Label(go.transform,id.Replace("Box ",""),Vector2.zero,new Color(1,.55f,.12f));
            }
            if(kind==ObjectKind.Terrain && phase==Phase.Blue && rect.width>1 && rect.height>.55f)
            {
                for(float x=.6f;x<rect.width-.3f;x+=1.7f)
                    for(float y=.4f;y<rect.height-.2f;y+=1.7f)
                        for(int petal=0;petal<4;petal++)
                        {
                            var mark=new GameObject("Indigo weave"); mark.transform.SetParent(go.transform,false);
                            float a=petal*Mathf.PI/2; mark.transform.localPosition=new Vector3(x-rect.width/2+Mathf.Cos(a)*.10f,y-rect.height/2+Mathf.Sin(a)*.10f,0);
                            mark.transform.localRotation=Quaternion.Euler(0,0,45);
                            var sr=mark.AddComponent<SpriteRenderer>(); sr.sprite=Square; sr.drawMode=SpriteDrawMode.Sliced; sr.size=new Vector2(.10f,.10f); sr.color=new Color(.8f,.91f,.94f,.17f); sr.sortingOrder=2;
                        }
            }
            if(kind==ObjectKind.Terrain && phase==Phase.White)
            {
                var edge=new GameObject("White road edge"); edge.transform.SetParent(go.transform,false); edge.transform.localPosition=new Vector3(0,-rect.height/2+.012f,0);
                var sr=edge.AddComponent<SpriteRenderer>(); sr.sprite=Square; sr.drawMode=SpriteDrawMode.Sliced; sr.size=new Vector2(rect.width,.024f); sr.color=new Color(.27f,.43f,.53f,.55f); sr.sortingOrder=4;
            }
            if(kind==ObjectKind.Terrain && phase==Phase.White && rect.height>.4f)
            {
                for(float x=Mathf.Ceil(rect.xMin);x<rect.xMax;x+=1) DecorationLine(go.transform,new Vector2(x-rect.center.x,0),new Vector2(.012f,rect.height));
                for(float y=Mathf.Ceil(rect.yMin);y<rect.yMax;y+=1) DecorationLine(go.transform,new Vector2(0,y-rect.center.y),new Vector2(rect.width,.012f));
            }
            return o;
        }
        static void DecorationLine(Transform parent,Vector2 local,Vector2 size)
        {
            var go=new GameObject("White region paper grid"); go.transform.SetParent(parent,false); go.transform.localPosition=local;
            var sr=go.AddComponent<SpriteRenderer>(); sr.sprite=Square; sr.drawMode=SpriteDrawMode.Sliced; sr.size=size; sr.sortingOrder=4; sr.color=new Color(.67f,.76f,.79f,.24f);
        }
        static Sprite FlagSprite()
        {
            string path=Root+"Data/IndigoFlag.png";
            if(!File.Exists(path))
            {
                var t=new Texture2D(32,32); var pixels=new Color[1024];
                for(int y=0;y<32;y++) for(int x=0;x<32;x++) pixels[y*32+x]=x<=31-Mathf.Abs(y-15.5f)*1.5f ? Color.white:Color.clear;
                t.SetPixels(pixels); t.Apply(); File.WriteAllBytes(path,t.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(t); AssetDatabase.ImportAsset(path);
            }
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            if(importer.textureType!=TextureImporterType.Sprite || importer.spritePixelsPerUnit!=32)
            { importer.textureType=TextureImporterType.Sprite; importer.spritePixelsPerUnit=32; importer.filterMode=FilterMode.Point; importer.SaveAndReimport(); }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        static void Label(Transform parent,string text,Vector2 offset,Color color)
        {
            var go=new GameObject("Label "+text); go.transform.SetParent(parent,false); go.transform.localPosition=new Vector3(offset.x,offset.y,-.1f);
            var label=go.AddComponent<TextMesh>(); label.text=text; label.anchor=TextAnchor.MiddleCenter; label.alignment=TextAlignment.Center;
            label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); label.fontSize=48; label.characterSize=.07f; label.color=color;
            var renderer=go.GetComponent<MeshRenderer>(); renderer.sharedMaterial=label.font.material; renderer.sortingOrder=14;
        }
        public static LevelRuntime Create(LevelDefinition definition)
        {
            var root=new GameObject("Greybox "+definition.levelId); var world=root.AddComponent<LevelRuntime>(); world.definition=definition;
            foreach(var s in definition.terrain)
            { var o=Make(root.transform,s.id,s.rect,s.phase,s.kind,s.elastic); o.breakable=s.breakable; }
            foreach(var b in definition.boxes) Make(root.transform,b.id,Rules.RectAt(b.center,Vector2.one*Rules.BoxSize),b.phase,ObjectKind.Box,true);
            Make(root.transform,"Player",Rules.RectAt(definition.spawn,new Vector2(Rules.PlayerWidth,Rules.PlayerHeight)),Phase.Blue,ObjectKind.Player);
            foreach(var z in definition.zones)
            {
                var go=new GameObject(z.id); go.transform.SetParent(root.transform,false); go.transform.position=new Vector3(z.x+z.width/2,z.floor,0);
                var sr=go.AddComponent<SpriteRenderer>(); sr.sprite=Square; sr.drawMode=SpriteDrawMode.Sliced; sr.size=new Vector2(z.width,.09f); sr.color=z.green ? new Color(.16f,1,.32f) : new Color(1,.82f,.08f); sr.sortingOrder=8;
                go.AddComponent<TransferZone2D>().data=z;
                Label(go.transform,z.id,new Vector2(0,.25f),sr.color);
            }
            var goal=new GameObject("Goal flag (blue)"); goal.transform.SetParent(root.transform,false); goal.transform.position=definition.goal;
            var flag=goal.AddComponent<SpriteRenderer>(); flag.sprite=FlagSprite(); flag.drawMode=SpriteDrawMode.Simple; goal.transform.localScale=new Vector3(.7f,.7f,1); flag.color=new Color(.035f,.23f,.38f); flag.sortingOrder=7; world.Goal=goal.transform;
            var pole=new GameObject("Flag pole"); pole.transform.SetParent(goal.transform,false); pole.transform.localPosition=new Vector3(-.45f,-.3f,0);
            var poleSprite=pole.AddComponent<SpriteRenderer>(); poleSprite.sprite=Square; poleSprite.drawMode=SpriteDrawMode.Sliced; poleSprite.size=new Vector2(.06f,1.2f); poleSprite.color=new Color(.035f,.23f,.38f); poleSprite.sortingOrder=7;
            var cameraGo=new GameObject("Main Camera"); cameraGo.transform.SetParent(root.transform,false); cameraGo.tag="MainCamera"; cameraGo.transform.position=new Vector3(12,6,-10);
            var camera=cameraGo.AddComponent<Camera>(); camera.orthographic=true; camera.orthographicSize=7.6f; camera.backgroundColor=new Color(.98f,.98f,.96f); camera.clearFlags=CameraClearFlags.SolidColor; world.ViewCamera=camera;
            var paper=root.AddComponent<PaperBackdrop>(); paper.square=Square;
            var realm=root.AddComponent<RealmVisuals>(); realm.world=world; realm.square=Square;
            root.AddComponent<DebugOverlay>().world=world;
            if(definition.mirrorInspection) root.AddComponent<MirrorInspection>().world=world;
            world.Initialize(); return world;
        }
        public static void Boundaries(LevelDefinition d)
        {
            d.terrain.Add(new ShapeSpec("Bottom",new Rect(0,0,24,.10f),Phase.Neutral));
            d.terrain.Add(new ShapeSpec("Top",new Rect(0,11.90f,24,.10f),Phase.Neutral));
            d.terrain.Add(new ShapeSpec("Left",new Rect(0,0,.10f,12),Phase.Neutral));
            d.terrain.Add(new ShapeSpec("Right",new Rect(23.90f,0,.10f,12),Phase.Neutral));
        }
        public static LevelDefinition SaveDefinition(LevelDefinition source,string name)
        {
            string path=Root+"Data/Levels/"+name+".asset"; var existing=AssetDatabase.LoadAssetAtPath<LevelDefinition>(path);
            if(existing) { EditorUtility.CopySerialized(source,existing); UnityEngine.Object.DestroyImmediate(source); EditorUtility.SetDirty(existing); }
            else { AssetDatabase.CreateAsset(source,path); existing=source; }
            AssetDatabase.SaveAssets(); return existing;
        }
        public static void SaveScene(LevelDefinition definition,string name)
        {
            var previous=SceneManager.GetActiveScene();
            if(Application.isBatchMode && string.IsNullOrEmpty(previous.path) && !previous.isDirty) previous=EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene); var world=Create(definition);
                // Explicit unlit sprites preserve the existing URP 2D pipeline and need no new lights.
                var unlit=Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                if(unlit)
                {
                    string path=Root+"Data/GreyboxUnlit.mat"; var m=AssetDatabase.LoadAssetAtPath<Material>(path);
                    if(!m) { m=new Material(unlit); AssetDatabase.CreateAsset(m,path); }
                    foreach(var sr in world.GetComponentsInChildren<SpriteRenderer>()) sr.sharedMaterial=m;
                }
                EditorSceneManager.SaveScene(scene,Root+"Scenes/"+name+".unity");
                foreach(var o in world.Objects.Where(o=>o.kind==ObjectKind.Player || o.kind==ObjectKind.Box).GroupBy(o=>o.kind).Select(g=>g.First()))
                    PrefabUtility.SaveAsPrefabAsset(o.gameObject,Root+"Prefabs/"+o.kind+".prefab");
            }
            finally { if(previous.IsValid()) SceneManager.SetActiveScene(previous); EditorSceneManager.CloseScene(scene,true); }
        }
        [MenuItem("Tools/YiChiLanRan/1a Generate minimal mirror inspection")]
        public static void MirrorInspectionScene()
        {
            var d=ScriptableObject.CreateInstance<LevelDefinition>(); d.levelId="Mirror inspection"; d.mirrorInspection=true;
            d.spawn=new Vector2(5,3.90f); d.goal=new Vector2(-50,-50);
            d.terrain.Add(new ShapeSpec("Reference platform",new Rect(4,3,14,.5f),Phase.Blue,true));
            d.boxes.Add(new BoxSpec("Box 1",9,3.52f,Phase.Blue));
            d.zones.Add(new ZoneSpec("Yellow",12,2,3.5f,false));
            d=SaveDefinition(d,"MirrorInspection"); SaveScene(d,"MirrorInspection");
        }
        [MenuItem("Tools/YiChiLanRan/1 Generate mechanics sandbox")]
        public static void Sandbox()
        {
            var d=ScriptableObject.CreateInstance<LevelDefinition>(); d.levelId="Mechanics sandbox"; d.spawn=new Vector2(2,2.50f); d.goal=new Vector2(22,3);
            Boundaries(d);
            d.terrain.Add(new ShapeSpec("Blue floor",new Rect(.1f,.1f,23.8f,1.9f),Phase.Blue,true));
            d.terrain.Add(new ShapeSpec("White ceiling",new Rect(.1f,2,23.8f,9.9f),Phase.White));
            d.boxes.Add(new BoxSpec("Box 1",4,2,Phase.Blue)); d.boxes.Add(new BoxSpec("Box 2",15,.12f,Phase.White));
            d.zones.Add(new ZoneSpec("Yellow",7,2,2,false)); d.zones.Add(new ZoneSpec("Green",13,4,2,true));
            d=SaveDefinition(d,"MechanicsSandbox"); SaveScene(d,"MechanicsSandbox");
            Debug.Log("[Greybox] Mechanics sandbox generated."); GreyboxChecks.Run();
        }
        [MenuItem("Tools/YiChiLanRan/4 Configure demo player")]
        public static void Configure()
        {
            PlayerSettings.defaultScreenWidth=1920; PlayerSettings.defaultScreenHeight=1080;
            PlayerSettings.defaultIsNativeResolution=false;
            PlayerSettings.resizableWindow=true; PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
            Time.fixedDeltaTime=Rules.Step;
            var paths=new[]{Root+"Scenes/Level_2_1.unity",Root+"Scenes/Level_2_2.unity"};
            var old=EditorBuildSettings.scenes;
            EditorBuildSettings.scenes=paths.Where(File.Exists).Select(p=>new EditorBuildSettingsScene(p,true)).Concat(old.Where(s=>!paths.Contains(s.path))).ToArray();
            AssetDatabase.SaveAssets();
        }
    }
}








