using UnityEngine;
namespace YiChiLanRan.Editor
{
    // Level 2.2 only. Target white solids follow the user's corrected silhouette.
    public static class Level22Layout
    {
        public const float WhiteRightJunction=16.4f;
        public const float Box2PitLeft=12.2f, Box2PitWidth=1.16f;
        public const float BlueMainTop=6.1f, BluePitCrownTop=7.1f, BlueRightTop=4.1f;
        public const float BlueConnectionBottom=2.7f, BlueConnectionOverlap=.4f;
        public const float FlagLedgeBottom=5.65f, FlagLedgeTop=6.85f;
        public const float HangingPillarLeft=WhiteRightJunction, HangingPillarRight=18.7f, HangingPillarBottom=BlueMainTop;
        static void Blue(LevelDefinition d,string id,float x,float y,float w,float h)
        { d.terrain.Add(new ShapeSpec(id,new Rect(x,y,w,h),Phase.Blue,true)); }
        static void White(LevelDefinition d,string id,float x,float bottom,float w,float top)
        { d.terrain.Add(new ShapeSpec(id,Rules.Mirror(new Rect(x,bottom,w,top-bottom),d.mirrorAxis),Phase.White)); }
        public static LevelDefinition Create()
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
            // Keep the recognisable notch, but give a full square box clearance underneath.
            Blue(d,"Green 1 lower lip",5.1f,4.3f,1.1f,BlueMainTop-4.3f);
            Blue(d,"Green 1 underside notch",6.2f,4.7f,1.2f,BlueMainTop-4.7f);
            Blue(d,"Green 1 right arm",7.4f,4.4f,1.9f,BlueMainTop-4.4f);
            // A longer crown and a level upper face allow white box 2 to be pushed after mirror 1.
            Blue(d,"Central box 2 crown",9.3f,4.9f,2.8f,1.2f);
            Blue(d,"Central middle body",10.3f,BlueConnectionBottom,WhiteRightJunction-10.3f,BlueMainTop-BlueConnectionBottom);
            // A low white pit must have the corresponding HIGH blue crown at identical X.
            Blue(d,"Box 2 pit counterpart crown",Box2PitLeft,BlueMainTop,Box2PitWidth,BluePitCrownTop-BlueMainTop);
            Blue(d,"Central descending stem",12.8f,.1f,1.3f,3.9f);
            // The two blue solids share a 0.40 x 1.40 volume, not a 0.10-high seam.
            float rightStart=WhiteRightJunction-BlueConnectionOverlap;
            Blue(d,"Right lower arm",rightStart,2.1f,23.9f-rightStart,BlueRightTop-2.1f);
            var flagLedge=new Rect(20,FlagLedgeBottom,3.9f,FlagLedgeTop-FlagLedgeBottom);
            Blue(d,"Right upper flag ledge",flagLedge.x,flagLedge.y,flagLedge.width,flagLedge.height);
            Blue(d,"Right wall",23.2f,4.1f,.7f,1.95f);
            d.terrain.Add(new ShapeSpec("Vertical box-only barrier",new Rect(17.2f,7.1f,1.5f,4.8f),Phase.Neutral,false,ObjectKind.BoxBarrier));
            // Keep the original box-only rule, but add a visible BLUE shell that also blocks
            // blue players. The lower-left corner touches the main platform's upper-right
            // corner: no low ceiling over the box-pushing area, and no walk-through gap.
            Blue(d,"Blue hanging pillar shell",HangingPillarLeft,HangingPillarBottom,HangingPillarRight-HangingPillarLeft,11.9f-HangingPillarBottom);

            // The white world's island cutout uses this SAME ledge, including its lowered ceiling.
            var holes=new[]{Rules.Mirror(flagLedge,d.mirrorAxis)};
            WhiteRealmGeometry.Add(d,"White left region",.1f,4.6f,3.56f,holes);
            float clearance=Rules.BoxSize+2*Rules.Skin;
            float middleFloor=2*d.mirrorAxis-BlueMainTop-clearance;
            float pitFloor=2*d.mirrorAxis-BluePitCrownTop-clearance;
            float rightFloor=2*d.mirrorAxis-BlueRightTop-clearance;
            WhiteRealmGeometry.Add(d,"White approach region",4.7f,Box2PitLeft-4.7f,middleFloor,holes);
            WhiteRealmGeometry.Add(d,"White box 2 pit",Box2PitLeft,Box2PitWidth,pitFloor,holes);
            // Extend the box-2 road another 1.10 towards Green 2. Both collision volumes
            // share this exact edge; use box 1 to climb the 2-unit step, as before.
            float pitRight=Box2PitLeft+Box2PitWidth;
            WhiteRealmGeometry.Add(d,"White middle region",pitRight,WhiteRightJunction-pitRight,middleFloor,holes);
            WhiteRealmGeometry.Add(d,"White right region",WhiteRightJunction,23.9f-WhiteRightJunction,rightFloor,holes);

            // Upper white silhouette missing from the previous version. Its lower edge leaves
            // 2.34 units over Green 1's road, enough for a box and an upright character on top.
            White(d,"White upper left pillar",.7f,4.6f,2.6f,10.95f);
            White(d,"White upper left shoulder",3.3f,4.6f,1.4f,9.45f);
            White(d,"White upper starter step",4.7f,7.2f,.3f,9.45f);
            White(d,"White upper starter notch",5,7.2f,.6f,9.85f);
            White(d,"White upper second step",5.6f,7.2f,2.5f,9.45f);
            White(d,"White upper yellow approach",8.1f,7.2f,2.2f,10.5f);
            White(d,"White upper yellow shelf",10.3f,7.2f,2.5f,10.95f);
            White(d,"White right top region",14.1f,9.9f,9.8f,11.9f);
            White(d,"White right hanging wall",14.1f,7.9f,1.2f,9.9f);

            d.boxes.Add(new BoxSpec("Box 1",3.9f,7.42f,Phase.Blue));
            d.boxes.Add(new BoxSpec("Box 2",9.8f,6.12f,Phase.White));
            d.boxes.Add(new BoxSpec("Box 3",21.3f,4.12f,Phase.White));
            d.zones.Add(new ZoneSpec("Yellow",10.4f,2.3f,1.05f,false) {hasMirroredFloor=true,mirroredFloor=10.95f});
            d.zones.Add(new ZoneSpec("Green 1",5.3f,3.1f,6.1f,true) {hasMirroredFloor=true,mirroredFloor=middleFloor});
            d.zones.Add(new ZoneSpec("Green 2",16.6f,3.1f,4.1f,true) {hasMirroredFloor=true,mirroredFloor=rightFloor});
            d.routeNotes="Corrected white upper silhouette and blue corridor from 2026-11-09 drawing. Box 2 starts on an extended 2.8-wide crown, lowered by 1.0 for a strict mirrored position on the white road; push it RIGHT into the new 1.16-wide pit at x=12.2..13.36 after yellow. Box 2 no longer starts as an automatic filler. Green 1 white corridor clearance 2.34. Extend white box-2 road from x=15.3 to x=16.4, exactly adjoining Green 2's right platform; move box 1 farther right and use it to climb the unchanged 2.0 step. All objects still use the common axis y=6; no box snapping or level-specific mirror formula. Level 2.1 unchanged.";
            d.routeNotes+=" Blue geometry now shares the white pit X profile (raised crown at x=12.2..13.36) and extended junction x=16.4. White floor heights derive from those blue tops plus the established box clearance, not separate unmatched constants.";
            d.routeNotes+=" Blue main/right bodies now overlap 0.40 horizontally and 1.40 vertically; Green 1's narrowest blue band is 1.40 high. Main body bottom=2.70, Green 1 notch bottom=4.70. Portal tops and white floors unchanged.";
            d.routeNotes+=" Thicken flag ledge downwards: bottom=5.65, top=6.85. Initial box 3 top=5.12 leaves 0.53 overhead, less than player height 0.76. White island cutout derives from the exact same rect (y=5.15..6.35). Move box 3 left out from under this ledge before using it to ascend.";
            d.routeNotes+=" BLUE hanging pillar shell x=16.4..18.7, y=6.1..11.9 preserves the separate box-only barrier. Lower-left corner meets main platform upper-right corner at (16.4,6.1), with zero horizontal overlap and zero vertical gap. No ceiling over box 1's pushing area. Shell strictly mirrors to y=0.1..5.9 and remains BLUE; approved white route uses normal opposite-phase filtering.";
            return d;
        }
    }
}

