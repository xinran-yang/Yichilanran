using UnityEditor;
using UnityEngine;
namespace YiChiLanRan.Editor
{
    public static class LevelCatalog
    {
        static void Blue(LevelDefinition d,string id,float x,float y,float width,float height)
        { d.terrain.Add(new ShapeSpec(id,new Rect(x,y,width,height),Phase.Blue,true)); }
        static void WhiteAfterMirror(LevelDefinition d,string id,Rect target)
        { d.terrain.Add(new ShapeSpec(id,Rules.Mirror(target,d.mirrorAxis),Phase.White)); }
        public static LevelDefinition Level21()
        {
            var d=ScriptableObject.CreateInstance<LevelDefinition>(); d.levelId="2.1"; d.nextScene="Level_2_2";
            const float startTop=1.25f,mainTop=2f,pitTop=.95f,greenTop=3.7f;
            const float pitLeft=13.1f,pitRight=17.7f,middleTop=6.45f,flagTop=8.15f;
            d.spawn=new Vector2(1,startTop+.4f); d.goal=new Vector2(22.1f,flagTop+.58f);
            GreyboxBuilder.Boundaries(d);
            Blue(d,"Starting blue floor",.1f,.1f,4.3f,startTop-.1f);
            Blue(d,"Main blue plateau",4.4f,.1f,pitLeft-4.4f,mainTop-.1f);
            Blue(d,"Recess bottom",pitLeft,.1f,pitRight-pitLeft,pitTop-.1f);
            Blue(d,"Right blue plateau",pitRight,.1f,23.9f-pitRight,greenTop-.1f);
            Blue(d,"Independent middle blue ledge",11.8f,middleTop-.65f,6.5f,.65f);
            Blue(d,"Upper flag ledge",16.1f,flagTop-.65f,7.4f,.65f);
            var holes=new[]{Rules.Mirror(new Rect(11.8f,middleTop-.65f,6.5f,.65f),d.mirrorAxis),Rules.Mirror(new Rect(16.1f,flagTop-.65f,7.4f,.65f),d.mirrorAxis)};
            float boxClearance=Rules.BoxSize+2*Rules.Skin;
            WhiteRealmGeometry.Add(d,"White start region",.1f,4.3f,2*d.mirrorAxis-startTop-boxClearance,holes);
            WhiteRealmGeometry.Add(d,"White main region",4.4f,pitLeft-4.4f,2*d.mirrorAxis-mainTop-boxClearance,holes);
            WhiteRealmGeometry.Add(d,"White central column",pitLeft,pitRight-pitLeft,2*d.mirrorAxis-pitTop,holes);
            WhiteRealmGeometry.Add(d,"White right region",pitRight,23.9f-pitRight,2*d.mirrorAxis-greenTop-boxClearance,holes);
            d.boxes.Add(new BoxSpec("Box 1",3.1f,startTop+Rules.Skin,Phase.Blue));
            d.boxes.Add(new BoxSpec("Box 2",6.9f,mainTop+Rules.Skin,Phase.White));
            d.boxes.Add(new BoxSpec("Box 3",19,greenTop+Rules.Skin,Phase.White));
            d.zones.Add(new ZoneSpec("Yellow",5.3f,3.1f,mainTop,false) {hasMirroredFloor=true,mirroredFloor=2*d.mirrorAxis-mainTop-boxClearance});
            d.zones.Add(new ZoneSpec("Green",20.3f,3.2f,greenTop,true) {hasMirroredFloor=true,mirroredFloor=2*d.mirrorAxis-greenTop-boxClearance});
            d.routeNotes="No extra white relay. Lower yellow-side blue plateau to 2.0; white floor follows the same source dimensions. Pit width 4.6 and right green rise 2.75 prevent direct ordinary/bounce exit and main-to-green shortcuts. Raise existing right-hand climbing ledges together by 0.7 to retain the blue-box-3 ascent. Start floor thickened one box-height. White terrain fill, collision and ink boundary are one geometry; flag reflects complete visual hierarchy.";
            return d;
        }
        public static LevelDefinition Level22() => Level22Layout.Create();
        [MenuItem("Tools/YiChiLanRan/2 Generate level 2.1")]
        public static void Generate21()
        { var d=GreyboxBuilder.SaveDefinition(Level21(),"Level_2_1"); GreyboxBuilder.SaveScene(d,"Level_2_1"); }
        [MenuItem("Tools/YiChiLanRan/3 Generate level 2.2")]
        public static void Generate22()
        { var d=GreyboxBuilder.SaveDefinition(Level22(),"Level_2_2"); GreyboxBuilder.SaveScene(d,"Level_2_2"); }
        [MenuItem("Tools/YiChiLanRan/0 Generate complete greybox demo")]
        public static void GenerateAll()
        { GreyboxBuilder.MirrorInspectionScene(); Generate21(); Generate22(); GreyboxBuilder.Configure(); }
        public static void RebuildReferences()
        {
            System.IO.Directory.CreateDirectory("DevReports");
            GenerateAll();
            System.IO.File.WriteAllText("DevReports/Reference-2.1.json",JsonUtility.ToJson(Level21(),true));
            System.IO.File.WriteAllText("DevReports/Reference-2.2.json",JsonUtility.ToJson(Level22(),true));
        }
    }
}




