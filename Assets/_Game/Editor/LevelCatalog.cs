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
            d.zones.Add(new ZoneSpec("Yellow",5.3f,3.1f,mainTop,false));
            d.zones.Add(new ZoneSpec("Green",20.3f,3.2f,greenTop,true));
            d.routeNotes="No extra white relay. Lower yellow-side blue plateau to 2.0; white floor follows the same source dimensions. Pit width 4.6 and right green rise 2.75 prevent direct ordinary/bounce exit and main-to-green shortcuts. Raise existing right-hand climbing ledges together by 0.7 to retain the blue-box-3 ascent. Start floor thickened one box-height. White terrain fill, collision and ink boundary are one geometry; flag reflects complete visual hierarchy.";
            return d;
        }
        public static LevelDefinition Level22()
        {
            var d=ScriptableObject.CreateInstance<LevelDefinition>(); d.levelId="2.2";
            d.spawn=new Vector2(1.2f,1.45f); d.goal=new Vector2(22.1f,7.43f);
            GreyboxBuilder.Boundaries(d);
            Blue(d,"Lower starting floor",.1f,.1f,3.2f,.95f);
            Blue(d,"First blue stair",3.3f,.1f,1.7f,2.45f);
            Blue(d,"Shallow starter notch",5,.1f,.6f,2.05f);
            Blue(d,"Second blue stair",5.6f,.1f,2.5f,2.45f);
            Blue(d,"Approach to yellow",8.1f,.1f,2.2f,1.4f);
            Blue(d,"Yellow low shelf",10.3f,.1f,2.5f,.95f);
            Blue(d,"Left upper shelf",.1f,6.5f,5.2f,.9f);
            Blue(d,"Left wall",.1f,1.05f,.6f,5.45f);
            Blue(d,"Green 1 lower lip",5.1f,4.9f,1.1f,1.2f);
            Blue(d,"Green 1 underside notch",6.2f,5.45f,1.2f,.65f);
            Blue(d,"Green 1 right arm",7.4f,5.15f,1.9f,.95f);
            Blue(d,"Central box 2 crown",9.3f,4.9f,1.0f,2.2f);
            Blue(d,"Central middle body",10.3f,4.0f,5.0f,2.1f);
            Blue(d,"Central descending stem",12.8f,.1f,1.3f,3.9f);
            Blue(d,"Right lower arm",15.2f,2.1f,8.7f,2.0f);
            Blue(d,"Right upper flag ledge",20,6.05f,3.9f,.8f);
            Blue(d,"Right wall",23.2f,4.1f,.7f,1.95f);
            d.terrain.Add(new ShapeSpec("Vertical box-only barrier",new Rect(17.2f,7.1f,1.5f,4.8f),Phase.Neutral,false,ObjectKind.BoxBarrier));
            var holes22=new[]{Rules.Mirror(new Rect(20,6.05f,3.9f,.8f),d.mirrorAxis)};
            WhiteRealmGeometry.Add(d,"White left region",.1f,4.6f,3.56f,holes22);
            WhiteRealmGeometry.Add(d,"White approach region",4.7f,4.56f,4.86f,holes22);
            WhiteRealmGeometry.Add(d,"White pit region",9.26f,1.08f,3.86f,holes22);
            WhiteRealmGeometry.Add(d,"White middle region",10.34f,4.96f,4.86f,holes22);
            WhiteRealmGeometry.Add(d,"White right region",15.3f,8.6f,6.86f,holes22);
            d.boxes.Add(new BoxSpec("Box 1",3.9f,7.42f,Phase.Blue));
            d.boxes.Add(new BoxSpec("Box 2",9.8f,7.12f,Phase.White));
            d.boxes.Add(new BoxSpec("Box 3",21.3f,4.12f,Phase.White));
            d.zones.Add(new ZoneSpec("Yellow",10.4f,2.3f,1.05f,false));
            d.zones.Add(new ZoneSpec("Green 1",5.3f,3.1f,6.1f,true));
            d.zones.Add(new ZoneSpec("Green 2",16.6f,3.1f,4.1f,true));
            d.routeNotes="Reference 2.2.1map.png / 2.2.2map.jpg + oral route. Yellow -> green 1 solo -> push blue 1 off upper shelf -> green 1 carry -> white 1 across white 2's filled gap -> white right ledge -> move white 3 out from below flag to green 2 -> blue 3 -> flag. Crown height calibrated so box 2 top fills the 1.08-wide white-road gap after strict mirror. Upper barrier is box-only. Flag lower face leaves 0.17 above a standing player on initial box 3; move box out before final jump.";
            return d;
        }
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




